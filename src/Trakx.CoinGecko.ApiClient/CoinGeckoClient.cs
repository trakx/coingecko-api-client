using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Trakx.Utils.Apis;
using Trakx.Utils.Extensions;

namespace Trakx.CoinGecko.ApiClient;

public class CoinGeckoClient : ICoinGeckoClient
{
    private static readonly ILogger Logger =
        Log.Logger.ForContext(MethodBase.GetCurrentMethod()!.DeclaringType);

    private readonly IMemoryCache _cache;
    private readonly ICoinsClient _coinsClient;
    private readonly ISimpleClient _simpleClient;
    private Dictionary<string, string>? _idsBySymbolName;
    private readonly string? _typeName;

    public Dictionary<string, string> IdsBySymbolName
    {
        get
        {
            var idsBySymbolName = _idsBySymbolName ??= GetCoinList().GetAwaiter().GetResult()
                .ToDictionary(c => GetSymbolNameKey(c.Symbol, c.Name), c => c.Id);
            return idsBySymbolName;
        }
    }

    public Dictionary<string, CoinFullData> CoinFullDataByIds { get; }

    public CoinGeckoClient(
        IMemoryCache cache,
        ICoinsClient coinsClient,
        ISimpleClient simpleClient)
    {
        _cache = cache;
        _coinsClient = coinsClient;
        _simpleClient = simpleClient;

        CoinFullDataByIds = new Dictionary<string, CoinFullData>();
        _typeName = GetType().FullName;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetLatestPrice(string coinGeckoId, string quoteCurrencyId = "usd-coin")
    {
        Guard.Against.NullOrEmpty(quoteCurrencyId, nameof(quoteCurrencyId));

        var tickerDetails = await _simpleClient
            .PriceAsync($"{coinGeckoId},{quoteCurrencyId}", "usd")
            .ConfigureAwait(false);

        Logger.Debug("Received latest price {tickerDetails}", JsonSerializer.Serialize(tickerDetails));

        var price = tickerDetails.Result[coinGeckoId]["usd"];
        var conversionToQuoteCurrency = quoteCurrencyId == "usd"
            ? 1m
            : tickerDetails.Result[quoteCurrencyId]["usd"];

        return price / conversionToQuoteCurrency ?? 0m;
    }

    public async Task<string?> GetCoinGeckoIdFromSymbol(string symbol)
    {
        var coinList = await GetCoinList();

        var ids = coinList.Where(c =>
                c.Symbol.Equals(symbol, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
        return ids.Count == 1 ? ids[0].Id : null;
    }

    private async Task<decimal> GetUsdFxRate(string quoteCurrencyId, string date)
    {
        Guard.Against.NullOrWhiteSpace(quoteCurrencyId, nameof(quoteCurrencyId));

        var cacheKey = $"{_typeName}|usd-fx-rate|{quoteCurrencyId}|{date}";
        if (_cache.TryGetValue(cacheKey, out decimal cached)) return cached;

        var quoteResponse = await _coinsClient.HistoryAsync(quoteCurrencyId, date, false.ToString())
            .ConfigureAwait(false);

        var fxRate = quoteResponse.Result.Market_data is not null &&
                     quoteResponse.Result.Market_data.Current_price.ContainsKey(Constants.Usd) ?
            quoteResponse.Result.Market_data.Current_price[Constants.Usd] : default;

        if (fxRate == null)
        {
            Logger.Debug($"Current price for '{Constants.Usd}' in coin id '{quoteCurrencyId} for date '{date:dd-MM-yyyy}' is missing.");
            throw new FailedToRetrievePriceException($"Failed to retrieve price of {quoteCurrencyId} as of {date}");
        }

        var entry = _cache.CreateEntry(cacheKey);
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
        entry.Value = fxRate.Value;
        return fxRate.Value;
    }

    /// <inheritdoc />
    public async Task<MarketData?> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin")
    {
        var date = asOf.ToString("dd-MM-yyyy");
        var cacheKey = $"{_typeName}|market-data|{id}|{quoteCurrencyId}|{date}";
        if (_cache.TryGetValue(cacheKey, out MarketData cached)) return cached;

        var fullData = await _coinsClient
            .HistoryAsync(id, date, false.ToString())
            .ConfigureAwait(false);

        var fxRate = await GetUsdFxRate(quoteCurrencyId, date);

        if (fullData.Result.Market_data == null)
            return null;

        var marketData = new MarketData
        {
            AsOf = asOf,
            CoinId = fullData.Result.Id,
            CoinSymbol = fullData.Result.Symbol,
            MarketCap = fullData.Result.Market_data.Market_cap[Constants.Usd] / fxRate,
            Volume = fullData.Result.Market_data.Total_volume[Constants.Usd] / fxRate,
            Price = fullData.Result.Market_data.Current_price[Constants.Usd] / fxRate,
            QuoteCurrency = fullData.Result.Symbol
        };

        var entry = _cache.CreateEntry(cacheKey);
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
        entry.Value = marketData;

        return marketData;
    }

    private string GetSymbolNameKey(string symbol, string name)
    {
        return $"{symbol.ToLower()}|{name.ToLower()}";
    }

    public async Task<IReadOnlyList<CoinList>> GetCoinList()
    {
        var cacheKey = $"{_typeName}|coin-list";
        var result = await _cache.GetOrCreateAsync<ReadOnlyCollection<CoinList>>(cacheKey,
            async e =>
            {
                var coinList = await _coinsClient.ListAllAsync().ConfigureAwait(false);
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                return coinList.Result!.AsReadOnly();
            });
        return result;
    }

    public async Task<IDictionary<string, IDictionary<string, decimal?>>> GetAllPrices(IEnumerable<string> ids, string[]? vsCurrencies = null)
    {
        var coinsPrice = await _simpleClient.PriceAsync(ids.ToCsvList(true, true, quoted: false),
            (vsCurrencies ?? new[] { Constants.Usd }).ToCsvList(true, true, quoted: false)).ConfigureAwait(false);
        return coinsPrice.Result;
    }

    public async Task<IList<ExtendedPrice>> GetAllPricesExtended(
        IEnumerable<string> ids,
        string[]? vsCurrencies = null,
        bool includeMarketCap = false,
        bool include24HrVol = false)
    {
        var currencies = vsCurrencies ?? new[] { Constants.Usd };
        var coinPrices = await _simpleClient.PriceAsync(
                ids.ToCsvList(true, true, quoted: false),
                currencies.ToCsvList(true, true, quoted: false),
                include_market_cap: includeMarketCap.ToString().ToLower(),
                include_24hr_vol: include24HrVol.ToString().ToLower())
            .ConfigureAwait(false);

        var result = new List<ExtendedPrice>();
        foreach (var coinInfo in coinPrices.Result)
        {
            foreach (string currency in currencies)
            {
                coinInfo.Value.TryGetValue(currency, out var price);
                if (price.HasValue)
                {
                    coinInfo.Value.TryGetValue($"{currency}_market_cap", out var marketCap);
                    coinInfo.Value.TryGetValue($"{currency}_24h_vol", out var dailyVolume);
                    result.Add(new ExtendedPrice
                    {
                        CoinGeckoId = coinInfo.Key,
                        Currency = currency,
                        DailyVolume = dailyVolume,
                        MarketCap = marketCap,
                        Price = price.Value,
                    });
                }
            }
        }
        return result;
    }

    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(string id, string vsCurrency, DateTimeOffset start, DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        var range = await _coinsClient
            .RangeAsync(id, vsCurrency, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), cancellationToken)
            .ConfigureAwait(false);

        return BuildMarketData(id, vsCurrency, range);
    }

    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketData(string id, string vsCurrency, int days,
        CancellationToken cancellationToken)
    {
        var range = await _coinsClient
            .Market_chartAsync(id, vsCurrency, days.ToString(), "daily", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return BuildMarketData(id, vsCurrency, range);
    }

    public async Task<List<MarketData>> Search(string vsCurrency, string? ids = null, string? category = null, string? order = null, int? per_page = null, int? page = null, CancellationToken cancellationToken = default)
    {
        var result = await _coinsClient
            .MarketsAsync(vsCurrency, ids, category, order, per_page, page, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.Result.ConvertAll(x => new MarketData
        {
            CoinId = x.Id,
            Name = x.Name,
            MarketCap = x.Market_cap,
            Price = x.Current_price,
            CoinSymbol = x.Symbol,
            Volume = x.Total_volume,
            CirculatingSupply = x.Circulating_supply,
        });
    }

    #region [ Private methods ]

    private Dictionary<DateTimeOffset, MarketData> BuildMarketData(string id, string vsCurrency, Response<Range> range)
    {
        return Enumerable.Range(0, range.Result.Prices.Count).Select(i =>
                new { Index = i, Date = DateTimeOffset.FromUnixTimeMilliseconds((long)range.Result.Prices[i][0]) })
            .ToDictionary(d => d.Date,
                d => new MarketData
                {
                    AsOf = d.Date,
                    CoinId = id,
                    CoinSymbol = null,
                    MarketCap = (decimal)range.Result.Market_caps[d.Index][1],
                    Price = (decimal)range.Result.Prices[d.Index][1],
                    Volume = (decimal)range.Result.Total_volumes[d.Index][1],
                    QuoteCurrency = vsCurrency
                });
    }

    #endregion [ Private methods ]
}

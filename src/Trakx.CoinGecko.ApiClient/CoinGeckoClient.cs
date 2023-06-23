using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Trakx.CoinGecko.ApiClient.Models;
using Trakx.Common.Extensions;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public class CoinGeckoClient : ICoinGeckoClient
{
    internal const string MainQuoteCurrency = Constants.Usd;

    private static readonly TimeSpan DefaultCacheLifeSpan = TimeSpan.FromDays(1);

    private readonly IMemoryCache _cache;
    private readonly ICoinsClient _coinsClient;
    private readonly ISimpleClient _simpleClient;
    private Dictionary<string, string>? _idsBySymbolName;
    private readonly string? _typeName;

    private static readonly ILogger Logger = LoggerProvider.Create<CoinGeckoClient>();

    public Dictionary<string, string> IdsBySymbolName => _idsBySymbolName ??= GetIdsBySymbolName();

    public Dictionary<string, CoinFullData> CoinFullDataByIds { get; }

    public CoinGeckoClient(
        IMemoryCache cache,
        ICoinsClient coinsClient,
        ISimpleClient simpleClient)
    {
        _cache = cache;
        _coinsClient = coinsClient;
        _simpleClient = simpleClient;

        CoinFullDataByIds = new();
        _typeName = GetType().FullName;
    }

    /// <inheritdoc />
    public async Task<string?> GetCoinGeckoIdFromSymbol(string symbol)
    {
        var coinList = await GetCoinList();

        var ids = coinList
            .Where(c => c.Symbol.EqualsIgnoreCase(symbol))
            .ToList();

        if (ids.Count != 1) return null;

        return ids[0].Id;
    }

    /// <inheritdoc />
    public async Task<IList<CoinList>> GetCoinList(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|coin-list";

        return await GetFromCacheOrApi(cacheKey, async () =>
        {
            var coinList = await _coinsClient
                .ListAllAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return coinList.Content;
        });
    }

    public async Task<ISet<string>> GetSupportedQuoteCurrencies(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|supported-vs-currencies";

        return await GetFromCacheOrApi(cacheKey, async () =>
        {
            var response = await _simpleClient
                .Supported_vs_currenciesAsync(cancellationToken)
                .ConfigureAwait(false);

            var result = new HashSet<string>(response.Content, StringComparer.OrdinalIgnoreCase);
            return result;
        });
    }

    /// <summary>
    /// As of 2023-06-23, CoinGecko does not support USDc as a quote currency.
    /// <see href="https://api.coingecko.com/api/v3/simple/supported_vs_currencies"/>
    /// As such, we need to:
    /// <list type="bullet">
    /// <item><description>Use USD as the 'vs currency' in the API call</description></item>
    /// <item><description>Also get the price of the wanted quote currency</description></item>
    /// <item><description>Convert the prices to the wanted quote currency</description></item>
    /// </list>
    /// </summary>
    public async Task<decimal?> GetLatestPrice(
        string coinGeckoId,
        string quoteCurrencyId = Constants.UsdCoin,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(coinGeckoId);
        Guard.Against.NullOrWhiteSpace(quoteCurrencyId);

        var prices = await GetAllPrices(
            coinGeckoId.AsSingletonIEnumerable(),
            quoteCurrencyId.AsSingletonArray(),
            cancellationToken);

        var price = prices.GetPrice(coinGeckoId, quoteCurrencyId);
        return price;
    }

    /// <inheritdoc />
    public async Task<MultiplePrices> GetAllPrices(
        IEnumerable<string> ids,
        string[]? vsCurrencies = default,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(ids);

        var supportedCurrencies = await GetSupportedQuoteCurrencies(cancellationToken);

        (var baseIds, var quoteIds) = GetIdsForPriceQuery(ids, vsCurrencies, supportedCurrencies);

        var response = await _simpleClient
            .PriceAsync(baseIds, quoteIds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Received latest price {PriceAsyncResponse}", JsonSerializer.Serialize(response));
        }

        var prices = new MultiplePrices(response.Content);
        return prices;
    }

    /// <inheritdoc />
    public async Task<IList<ExtendedPrice>> GetAllPricesExtended(
        IEnumerable<string> ids,
        string[]? vsCurrencies = default,
        bool includeMarketCap = false,
        bool include24HrVol = false,
        CancellationToken cancellationToken = default)
    {
        if (vsCurrencies == null || vsCurrencies.Length == 0)
        {
            vsCurrencies = new[] { MainQuoteCurrency };
        }

        var supportedCurrencies = await GetSupportedQuoteCurrencies(cancellationToken);

        (var baseIds, var quoteIds) = GetIdsForPriceQuery(ids, vsCurrencies, supportedCurrencies);

        var coinPrices = await _simpleClient.PriceAsync(
                baseIds, quoteIds,
                include_market_cap: includeMarketCap.ToString().ToLower(),
                include_24hr_vol: include24HrVol.ToString().ToLower(),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var result = new List<ExtendedPrice>();

        foreach (var coinInfo in coinPrices.Content)
        {
            foreach (string currency in vsCurrencies)
            {
                coinInfo.Value.TryGetValue(currency, out var price);
                if (price == null) continue;

                coinInfo.Value.TryGetValue($"{currency}_market_cap", out var marketCap);
                coinInfo.Value.TryGetValue($"{currency}_24h_vol", out var dailyVolume);

                var extendedPrice = new ExtendedPrice
                {
                    CoinGeckoId = coinInfo.Key,
                    Currency = currency,
                    DailyVolume = dailyVolume,
                    MarketCap = marketCap,
                    Price = price.Value,
                };

                result.Add(extendedPrice);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<MarketData?> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = Constants.UsdCoin)
    {
        var date = asOf.ToString("dd-MM-yyyy");
        var cacheKey = $"{_typeName}|market-data|{id}|{quoteCurrencyId}|{date}";

        return await GetFromCacheOrApi(cacheKey, async () =>
        {
            var fullData = await _coinsClient
            .HistoryAsync(id, date, false.ToString())
            .ConfigureAwait(false);

            var content = fullData.Content;
            var data = content.Market_data;
            if (data == null) return null;

            var fxRate = await GetUsdFxRate(quoteCurrencyId, date);

            return new MarketData
            {
                AsOf = asOf,

                CoinId = content.Id,
                CoinSymbol = content.Symbol,
                QuoteCurrency = content.Symbol,

                MarketCap = data.Market_cap[MainQuoteCurrency] / fxRate,
                Volume = data.Total_volume[MainQuoteCurrency] / fxRate,
                Price = data.Current_price[MainQuoteCurrency] / fxRate,
            };
        });
    }

    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(
        string id, string vsCurrency,
        DateTimeOffset start, DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        var range = await _coinsClient
            .RangeAsync(id, vsCurrency, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), cancellationToken)
            .ConfigureAwait(false);

        return BuildMarketData(id, vsCurrency, range.Content);
    }

    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketData(
        string id, string vsCurrency, int days, CancellationToken cancellationToken = default)
    {
        var range = await _coinsClient
            .Market_chartAsync(id, vsCurrency, days.ToString(), "daily", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return BuildMarketData(id, vsCurrency, range.Content);
    }

    public async Task<List<MarketData>> Search(
        string vsCurrency,
        string? ids = null,
        string? category = null,
        string? order = null,
        int? per_page = null,
        int? page = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _coinsClient
            .MarketsAsync(vsCurrency, ids, category, order, per_page, page, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.Content.ConvertAll(x => new MarketData
        {
            CoinId = x.Id,
            Name = x.Name,
            MarketCap = x.Market_cap,
            Price = x.Current_price,
            CoinSymbol = x.Symbol,
            Volume = x.Total_volume,
            CirculatingSupply = x.Circulating_supply,
            MarketCapRank = x.Market_cap_rank,
        });
    }

    private static string GetSymbolNameKey(string symbol, string name)
    {
        return $"{symbol}|{name}".ToLower();
    }

    private static Dictionary<DateTimeOffset, MarketData> BuildMarketData(string id, string vsCurrency, Range range)
    {
        return Enumerable
            .Range(0, range.Prices.Count)
            .Select(i => new { Index = i, Date = DateTimeOffset.FromUnixTimeMilliseconds((long)range.Prices[i][0]) })
            .ToDictionary(d => d.Date,
                d => new MarketData
                {
                    AsOf = d.Date,
                    CoinId = id,
                    CoinSymbol = null,
                    MarketCap = (decimal)range.Market_caps[d.Index][1],
                    Price = (decimal)range.Prices[d.Index][1],
                    Volume = (decimal)range.Total_volumes[d.Index][1],
                    QuoteCurrency = vsCurrency
                });
    }

    private async Task<decimal> GetUsdFxRate(string quoteCurrencyId, string date)
    {
        Guard.Against.NullOrWhiteSpace(quoteCurrencyId);

        var cacheKey = $"{_typeName}|usd-fx-rate|{quoteCurrencyId}|{date}";

        return await GetFromCacheOrApi(cacheKey, async () =>
        {
            var quoteResponse = await _coinsClient
                .HistoryAsync(quoteCurrencyId, date, false.ToString())
                .ConfigureAwait(false);

            decimal? fxRate = null;
            var currentPrice = quoteResponse.Content.Market_data?.Current_price;
            currentPrice?.TryGetValue(MainQuoteCurrency, out fxRate);

            if (fxRate != null) return fxRate.Value;

            Logger.LogDebug($"Current price for '{MainQuoteCurrency}' in coin id '{quoteCurrencyId} for date '{date:dd-MM-yyyy}' is missing.");
            throw new FailedToRetrievePriceException($"Failed to retrieve price of {quoteCurrencyId} as of {date}");
        });
    }

    private async Task<T> GetFromCacheOrApi<T>(string cacheKey, Func<Task<T>> getFromApi)
    {
        var value = await _cache.GetOrCreateAsync<T>(cacheKey, async (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheLifeSpan;
            return await getFromApi();
        });

        return value!;
    }

    private Dictionary<string, string> GetIdsBySymbolName()
    {
        return GetCoinList()
            .GetAwaiter()
            .GetResult()
            .ToDictionary(c => GetSymbolNameKey(c.Symbol, c.Name), c => c.Id);
    }

    /// <summary>
    /// Each requested quote currency needs to be either a 'base' or a 'vs' id in the price call,
    /// depending if it's a supported quote currency or not.<br />
    /// This method ensures a valid list of 'base' and 'vs' ids
    /// according to the logic explained in the comment for <see cref="GetLatestPrice(string, string)"/>
    /// </summary>
    private static (string BaseIds, string QuoteIds) GetIdsForPriceQuery(
        IEnumerable<string> ids, string[]? vsCurrencies,
        ISet<string> supportedQuoteCurrencies)
    {
        List<string> baseIds = new();
        List<string> quoteIds = new();

        if (ids != null) baseIds.AddRange(ids);

        vsCurrencies ??= Array.Empty<string>();

        foreach (var id in vsCurrencies)
        {
            var isSupported = supportedQuoteCurrencies.Contains(id);
            if (isSupported) quoteIds.Add(id);
            else baseIds.Add(id);
        }

        // ensure at least one supported quote currency
        if (quoteIds.Count == 0)
        {
            quoteIds.Add(MainQuoteCurrency);
        }

        var baseList = baseIds.ToCsvList(distinct: true, toLower: true, quoted: false);
        var quoteList = quoteIds.ToCsvList(distinct: true, toLower: true, quoted: false);
        return (baseList, quoteList);
    }
}

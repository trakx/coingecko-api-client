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
using Trakx.Common.DateAndTime;
using Trakx.Common.Extensions;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public partial class CoinGeckoClient : ICoinGeckoClient
{
    internal const string MainQuoteCurrency = Constants.Usd;

    private readonly IMemoryCache _cache;
    private readonly ICoinsClient _coinsClient;
    private readonly ISimpleClient _simpleClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly string? _typeName;

    private static readonly ILogger Logger = LoggerProvider.Create<CoinGeckoClient>();

    public CoinGeckoClient(
        IMemoryCache cache,
        ICoinsClient coinsClient,
        ISimpleClient simpleClient,
        IDateTimeProvider dateTimeProvider)
    {
        _cache = cache;
        _coinsClient = coinsClient;
        _simpleClient = simpleClient;
        _dateTimeProvider = dateTimeProvider;
        _typeName = GetType().FullName;
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
            include_market_cap: includeMarketCap,
            include_24hr_vol: include24HrVol,
            cancellationToken: cancellationToken);

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
    public async Task<string?> GetCoinGeckoIdFromSymbol(string symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|coingeckoid-from|{symbol}";
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinGeckoIdFromSymbolFromApi(symbol, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IList<CoinList>> GetCoinList(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|coin-list";
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinListFromApi(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ICollection<string>> GetSupportedQuoteCurrencies(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|supported-vs-currencies";
        return await GetFromCacheOrApi(cacheKey, async () => await GetSupportedQuoteCurrenciesFromApi(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<MarketData?> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = Constants.UsdCoin)
    {
        var date = GetDateString(asOf);
        var cacheKey = $"{_typeName}|market-data|{id}|{quoteCurrencyId}|{date}";
        return await GetFromCacheOrApi(cacheKey, async () => await GetMarketDataAsOfFromIdFromApi(id, asOf, quoteCurrencyId, date));
    }

    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(
        string id, string vsCurrency,
        DateTimeOffset start, DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var startUnix = start.ToUnixTimeSeconds();
        var endUnix = end.ToUnixTimeSeconds();

        // skip cache for any query ending today or yesterday
        var endsHowLongAgo = _dateTimeProvider.UtcNowAsOffset.Date - end.Date;
        if (endsHowLongAgo.Days <= 1)
            return await GetMarketDataForDateRangeFromApi(id, vsCurrency, startUnix, endUnix, cancellationToken);

        var cacheKey = $"{_typeName}|range|{id}|{vsCurrency}|{startUnix}|{endUnix}";
        return await GetFromCacheOrApi(cacheKey, async () => await GetMarketDataForDateRangeFromApi(id, vsCurrency, startUnix, endUnix, cancellationToken));
    }

    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketData(
        string id, string vsCurrency, int days, CancellationToken cancellationToken = default)
    {
        // skip cache for queries for 1 day only
        if (days <= 1)
            return await GetMarketDataFromApi(id, vsCurrency, days, cancellationToken);

        var cacheKey = $"{_typeName}|chart|{id}|{vsCurrency}|{days}";
        return await GetFromCacheOrApi(cacheKey, async () => await GetMarketDataFromApi(id, vsCurrency, days, cancellationToken));
    }

    public async Task<List<MarketData>> Search(
        string vsCurrency = Constants.Usd,
        string? ids = null,
        string? category = null,
        string order = "market_cap_desc",
        int? per_page = null,
        int? page = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|search|{vsCurrency}|{ids}|{category}|{order}|{page}|{per_page}";
        return await GetFromCacheOrApi(cacheKey, async () => await SearchApi(vsCurrency, ids, category, order, per_page, page, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IList<MarketData>> GetMarketRank(int limit = ICoinGeckoClient.MarketRankDefaultLimit, CancellationToken cancellationToken = default)
    {
        // The default limit is 1000 coins in the results.
        // We can cache the calls to get 1000 ranked coins daily.
        // If a call is made to get a lower amount, say 50 coins, we grab the cached 1000 list and take only the first 50.
        // If a call is made for a rank of over 1000, it runs and is cached on its own.

        var limitCacheKey = Math.Max(limit, ICoinGeckoClient.MarketRankDefaultLimit);
        var cacheKey = $"{_typeName}|market-rank|{limitCacheKey}";

        var list = await GetFromCacheOrApi(cacheKey, async () => await GetMarketRankFromApi(limit, cancellationToken));

        // this ensures we only return the requested "limit" amount even when reusing the default "1000" result
        if (list.Count > limit)
            list = list.Take(limit).ToList();

        return list;
    }
}

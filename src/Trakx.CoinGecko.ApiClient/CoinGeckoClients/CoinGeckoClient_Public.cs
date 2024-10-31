using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Trakx.CoinGecko.ApiClient.Models;
using Trakx.Common.DateAndTime;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public partial class CoinGeckoClient : ICoinGeckoClient
{
    internal const string MainQuoteCurrency = Constants.Usd;

    private readonly IMemoryCache _cache;
    private readonly ICoinsClient _coinsClient;
    private readonly ISimpleClient _simpleClient;
    private readonly ISearchClient _searchClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly string? _typeName;

    private static readonly ILogger Logger = LoggerProvider.Create<CoinGeckoClient>();

    public CoinGeckoClient(
        IMemoryCache cache,
        ICoinsClient coinsClient,
        ISimpleClient simpleClient,
        ISearchClient searchClient,
        IDateTimeProvider dateTimeProvider)
    {
        _cache = cache;
        _coinsClient = coinsClient;
        _simpleClient = simpleClient;
        _searchClient = searchClient;
        _dateTimeProvider = dateTimeProvider;
        _typeName = GetType().FullName;
    }

    /// <inheritdoc />
    public async Task<decimal?> GetLatestPrice(
        string coinGeckoId,
        string quoteCurrencyId = Constants.UsdCoin,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coinGeckoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(quoteCurrencyId);

        var prices = await GetAllPrices(
            [coinGeckoId],
            [quoteCurrencyId],
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
        ArgumentNullException.ThrowIfNull(ids);

        var response = await GetAllPricesInternal(ids, vsCurrencies, cancellationToken);

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
            vsCurrencies = [MainQuoteCurrency];
        }

        (var baseIds, var quoteIds) = await GetIdsForPriceQuery(ids, vsCurrencies, cancellationToken);

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
    public async Task<PricesForSymbols> GetAllPricesForSymbols(
        IList<string> symbols,
        string[]? vsCurrencies = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        // Map each symbol e.g. "btc" to the corresponding coingeckoid e.g. "bitcoin".
        // The result is a map / lookup because some symbols can be mapped to multiple tokens, like "UNI".
        var symbolIdMap = await GetIdsFromSymbolsInternal(symbols, cancellationToken);

        var ids = symbolIdMap.SelectMany(p => p.Value).ToList();

        var priceResponse = await GetAllPricesInternal(ids, vsCurrencies, cancellationToken);

        return new PricesForSymbols()
        {
            // these prices are indexed by coingecko id
            Prices = new MultiplePrices(priceResponse.Content),

            // necessary so the caller can navigate symbol -> coingeckoid(s) -> price(s)
            SymbolToIdMap = symbolIdMap,
        };
    }


}

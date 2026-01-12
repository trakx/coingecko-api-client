using Trakx.Common.DateAndTime;

namespace Trakx.CoinGecko.ApiClient;

// will soon become CoinGeckoMarketClient
public partial class CoinGeckoClient : ICoinGeckoMarketClient
{
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <inheritdoc />
    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketData(
        string id,
        string vsCurrency,
        int days,
        CancellationToken cancellationToken = default)
    {
        // skip cache for queries for 1 day only
        if (days <= 1)
            return await GetMarketDataInternal(id, vsCurrency, days, cancellationToken);

        var cacheKey = BuildCacheKey(nameof(GetMarketData), id, vsCurrency, days);
        return await GetFromCacheOrApi(cacheKey, async () => await GetMarketDataInternal(id, vsCurrency, days, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(
        string id,
        string vsCurrency,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var startUnix = start.ToUnixTimeSeconds();
        var endUnix = end.ToUnixTimeSeconds();

        // skip cache for any query ending today or yesterday
        var endsHowLongAgo = _dateTimeProvider.UtcNowAsOffset.Date - end.Date;
        if (endsHowLongAgo.Days <= 1)
            return await GetMarketDataForDateRangeInternal(id, vsCurrency, startUnix, endUnix, cancellationToken);

        var cacheKey = BuildCacheKey(nameof(GetMarketDataForDateRange), id, vsCurrency, startUnix, endUnix);
        return await GetFromCacheOrApi(cacheKey, async () => await GetMarketDataForDateRangeInternal(id, vsCurrency, startUnix, endUnix, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<MarketData?> GetMarketDataAsOfFromId(
        string id,
        DateTime asOf,
        string quoteCurrencyId = Constants.UsdCoin,
        CancellationToken cancellationToken = default)
    {
        var date = asOf.ToDateString();

        var cacheKey = BuildCacheKey(nameof(GetMarketDataAsOfFromId), id, quoteCurrencyId, date);
        return await GetFromCacheOrApi(cacheKey, async () => await GetMarketDataAsOfFromIdInternal(id, asOf, quoteCurrencyId, date, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IList<MarketData>> GetMarketRank(
        int limit = ICoinGeckoMarketClient.MarketRankDefaultLimit,
        CancellationToken cancellationToken = default)
    {
        // The default limit is 1000 coins in the results.
        // We can cache the calls to get 1000 ranked coins daily.
        // If a call is made to get a lower amount, say 50 coins, we grab the cached 1000 list and take only the first 50.
        // If a call is made for a rank of over 1000, it runs and is cached on its own.

        var limitCacheKey = Math.Max(limit, ICoinGeckoMarketClient.MarketRankDefaultLimit);

        var cacheKey = BuildCacheKey(nameof(GetMarketRank), limitCacheKey);
        var list = await GetFromCacheOrApi(cacheKey, async () => await GetMarketRankInternal(limit, cancellationToken));

        // this ensures we only return the requested "limit" amount even when reusing the default "1000" result
        if (list.Count > limit)
            list = list.Take(limit).ToList();

        return list;
    }

    /// <inheritdoc />
    public async Task<List<MarketData>> Search(
        string vsCurrency = ICoinGeckoMarketClient.MainQuoteCurrency,
        string? ids = null,
        string? category = null,
        string order = ICoinGeckoMarketClient.DefaultSearchOrder,
        int? per_page = null,
        int? page = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(nameof(Search), vsCurrency, ids, category, order, page, per_page);
        return await GetFromCacheOrApi(cacheKey, async () => await SearchMarketsInternal(vsCurrency, ids, category, order, per_page, page, cancellationToken));
    }
}

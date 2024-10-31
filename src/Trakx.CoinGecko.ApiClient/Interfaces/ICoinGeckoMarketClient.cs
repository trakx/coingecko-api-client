namespace Trakx.CoinGecko.ApiClient;

public interface ICoinGeckoMarketClient
{
    internal const string MainQuoteCurrency = Constants.Usd;
    internal const string DefaultSearchOrder = "market_cap_desc";
    internal const int MarketRankDefaultLimit = 1000;

    /// <summary>Get historical market data include price, market cap, and 24h volume.</summary>
    Task<IDictionary<DateTimeOffset, MarketData>> GetMarketData(
        string id,
        string vsCurrency,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>Get historical market data include price, market cap, and 24h volume within a range of timestamp.</summary>
    Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(
        string id,
        string vsCurrency,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default);

    /// <summary>Get historical data (name, price, market, stats) at a given date for a coin.</summary>
    Task<MarketData?> GetMarketDataAsOfFromId(
        string id,
        DateTime asOf,
        string quoteCurrencyId = Constants.UsdCoin,
        CancellationToken cancellationToken = default);

    /// <summary>Get details for up to the top <paramref name="limit"/> coins with highest market cap.</summary>
    Task<IList<MarketData>> GetMarketRank(
        int limit = MarketRankDefaultLimit,
        CancellationToken cancellationToken = default);

    /// <summary>List all supported coins price, market cap, volume, and market related data.</summary>
    Task<List<MarketData>> Search(
        string vsCurrency = MainQuoteCurrency,
        string? ids = null,
        string? category = null,
        string order = DefaultSearchOrder,
        int? per_page = null,
        int? page = null,
        CancellationToken cancellationToken = default);
}
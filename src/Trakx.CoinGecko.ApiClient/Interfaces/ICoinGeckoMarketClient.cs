using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trakx.CoinGecko.ApiClient;

public interface ICoinGeckoMarketClient
{
    internal const string MainQuoteCurrency = Constants.Usd;
    internal const int MarketRankDefaultLimit = 1000;

    Task<IList<MarketData>> GetMarketRank(
        int limit = MarketRankDefaultLimit,
        CancellationToken cancellationToken = default);

    Task<MarketData?> GetMarketDataAsOfFromId(
        string id, DateTime asOf, string quoteCurrencyId = Constants.UsdCoin);

    Task<IDictionary<DateTimeOffset, MarketData>> GetMarketData(
        string id, string vsCurrency, int days,
        CancellationToken cancellationToken = default);

    Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(
        string id, string vsCurrency,
        DateTimeOffset start, DateTimeOffset end,
        CancellationToken cancellationToken = default);

    Task<List<MarketData>> Search(
        string vsCurrency = MainQuoteCurrency,
        string? ids = null,
        string? category = null,
        string order = "market_cap_desc",
        int? per_page = null,
        int? page = null,
        CancellationToken cancellationToken = default);
}
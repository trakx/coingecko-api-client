using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trakx.CoinGecko.ApiClient.Models;

namespace Trakx.CoinGecko.ApiClient;

public interface ICoinGeckoClient
{
    internal const string MainQuoteCurrency = Constants.Usd;
    internal const int MarketRankDefaultLimit = 1000;

    // symbol operations

    Task<string?> GetCoinGeckoIdFromSymbol(string symbol, CancellationToken cancellationToken = default);

    Task<IList<CoinList>> GetCoinList(CancellationToken cancellationToken = default);

    Task<ICollection<string>> GetSupportedQuoteCurrencies(CancellationToken cancellationToken = default);


    // price operations

    Task<decimal?> GetLatestPrice(
        string coinGeckoId,
        string quoteCurrencyId = Constants.UsdCoin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a collection of prices,
    /// grouped by each CoinGecko ID in <paramref name="ids"/>,
    /// then by each requested <paramref name="vsCurrencies"/>.
    /// </summary>
    Task<MultiplePrices> GetAllPrices(
        IEnumerable<string> ids,
        string[]? vsCurrencies = default,
        CancellationToken cancellationToken = default);

    Task<IList<ExtendedPrice>> GetAllPricesExtended(
        IEnumerable<string> ids,
        string[]? vsCurrencies = default,
        bool includeMarketCap = false,
        bool include24HrVol = false,
        CancellationToken cancellationToken = default);


    // market data

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
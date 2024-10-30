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

    /// <summary>Convert a token symbol to its corresponding CoinGecko Id.</summary>
    Task<string?> GetCoinGeckoIdFromSymbol(string symbol, CancellationToken cancellationToken = default);

    /// <summary>List all supported coins id, name and symbol (no pagination required)</summary>
    Task<IList<CoinList>> GetCoinList(CancellationToken cancellationToken = default);

    /// <summary>List all <see cref="Coins"/> which symbol match <paramref name="symbol"/> exactly.</summary>
    Task<List<Coins>> GetCoinsFromSymbol(string symbol, CancellationToken cancellationToken);

    /// <summary>Map all symbols of ranked tokens with their respective CoinGecko Ids.</summary>
    Task<SymbolToCoinGeckoIdsMap> MapRankedSymbolsToCoinGeckoIds(CancellationToken cancellationToken = default);

    /// <summary>Get list of supported quote currencies (vs_currencies).</summary>
    Task<ICollection<string>> GetSupportedQuoteCurrencies(CancellationToken cancellationToken = default);


    // price operations

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

    /// <summary>Returns a collection of extended prices.</summary>
    Task<IList<ExtendedPrice>> GetAllPricesExtended(
        IEnumerable<string> ids,
        string[]? vsCurrencies = default,
        bool includeMarketCap = false,
        bool include24HrVol = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a collection of prices similar to <see cref="GetAllPrices(IEnumerable{string}, string[]?, CancellationToken)"/>,
    /// first mapping the token symbols to any matching coingecko Ids.<br />
    /// Also returns a map between the provided <paramref name="symbols"/> and the found coingecko ids.
    /// </summary>
    Task<PricesForSymbols> GetAllPricesForSymbols(
        IList<string> symbols,
        string[]? vsCurrencies = null,
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
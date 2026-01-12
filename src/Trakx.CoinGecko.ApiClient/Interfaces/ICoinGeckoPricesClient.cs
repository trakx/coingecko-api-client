using Trakx.CoinGecko.ApiClient.Models;

namespace Trakx.CoinGecko.ApiClient;

public interface ICoinGeckoPricesClient
{
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
}
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trakx.Common.ApiClient;
using Trakx.Common.Extensions;

namespace Trakx.CoinGecko.ApiClient;

// will soon become CoinGeckoPricesClient
public partial class CoinGeckoClient
{
    private async Task<Response<IDictionary<string, PriceInMultipleCurrencies>>> GetAllPricesInternal(
        IEnumerable<string> ids,
        string[]? vsCurrencies,
        CancellationToken cancellationToken = default)
    {
        var (baseIds, quoteIds) = await GetIdsForPriceQuery(ids, vsCurrencies, cancellationToken);

        var response = await _simpleClient.PriceAsync(baseIds, quoteIds, cancellationToken: cancellationToken);

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Received latest price {PriceAsyncResponse}", JsonSerializer.Serialize(response));
        }

        return response;
    }

    /// <summary>
    /// Each requested quote currency needs to be either a 'base' or a 'vs' id in the price call,
    /// depending if it's a supported quote currency or not.<br />
    /// This method ensures a valid list of 'base' and 'vs' ids
    /// according to the logic explained in the comment for <see cref="GetLatestPrice(string,string,CancellationToken)"/>
    /// </summary>
    private async Task<(string BaseIds, string QuoteIds)> GetIdsForPriceQuery(
        IEnumerable<string> ids,
        string[]? vsCurrencies,
        CancellationToken cancellationToken = default)
    {
        List<string> baseIds = [];
        List<string> quoteIds = [];

        if (ids != null) baseIds.AddRange(ids);

        vsCurrencies ??= [];

        var supportedQuoteCurrencies = await GetSupportedQuoteCurrencies(cancellationToken);

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

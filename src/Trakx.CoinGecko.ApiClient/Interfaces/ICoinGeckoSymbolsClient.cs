using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trakx.CoinGecko.ApiClient;

public interface ICoinGeckoSymbolsClient
{
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
}
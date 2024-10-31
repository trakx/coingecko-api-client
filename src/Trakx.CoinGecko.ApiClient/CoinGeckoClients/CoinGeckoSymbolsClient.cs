namespace Trakx.CoinGecko.ApiClient;

// will soon become CoinGeckoSymbolsClient
public partial class CoinGeckoClient : ICoinGeckoSymbolsClient
{
    /// <inheritdoc />
    public async Task<IList<CoinList>> GetCoinList(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrApi(
            [nameof(GetCoinList)],
            async () => await GetCoinListInternal(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ICollection<string>> GetSupportedQuoteCurrencies(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrApi(
            [nameof(GetSupportedQuoteCurrencies)],
            async () => await GetSupportedQuoteCurrenciesInternal(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<string?> GetCoinGeckoIdFromSymbol(string symbol, CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrApi(
            [nameof(GetCoinGeckoIdFromSymbol), symbol],
            async () => await GetCoinGeckoIdFromSymbolInternal(symbol, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<List<Coins>> GetCoinsFromSymbol(string symbol, CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrApi(
            [nameof(GetCoinsFromSymbol), symbol],
            async () => await GetCoinsFromSymbolInternal(symbol, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SymbolToCoinGeckoIdsMap> MapRankedSymbolsToCoinGeckoIds(CancellationToken cancellationToken = default)
    {
        return await GetFromCacheOrApi(
            [nameof(MapRankedSymbolsToCoinGeckoIds)],
            async () => await MapRankedSymbolsToCoinGeckoIdsInternal(cancellationToken));
    }
}

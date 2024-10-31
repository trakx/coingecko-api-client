namespace Trakx.CoinGecko.ApiClient;

// will soon become CoinGeckoSymbolsClient
public partial class CoinGeckoClient : ICoinGeckoSymbolsClient
{
    /// <inheritdoc />
    public async Task<IList<CoinList>> GetCoinList(CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(nameof(GetCoinList));
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinListInternal(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ICollection<string>> GetSupportedQuoteCurrencies(CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(nameof(GetSupportedQuoteCurrencies));
        return await GetFromCacheOrApi(cacheKey, async () => await GetSupportedQuoteCurrenciesInternal(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<string?> GetCoinGeckoIdFromSymbol(string symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(nameof(GetCoinGeckoIdFromSymbol), symbol);
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinGeckoIdFromSymbolInternal(symbol, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<List<Coins>> GetCoinsFromSymbol(string symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(nameof(GetCoinsFromSymbol), symbol);
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinsFromSymbolInternal(symbol, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SymbolToCoinGeckoIdsMap> MapRankedSymbolsToCoinGeckoIds(CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(nameof(MapRankedSymbolsToCoinGeckoIds));
        return await GetFromCacheOrApi(cacheKey, async () => await MapRankedSymbolsToCoinGeckoIdsInternal(cancellationToken));
    }
}

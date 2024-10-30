namespace Trakx.CoinGecko.ApiClient;

public partial class CoinGeckoClient : ICoinGeckoSymbolsClient
{
    /// <inheritdoc />
    public async Task<IList<CoinList>> GetCoinList(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|coin-list";
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinListInternal(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ICollection<string>> GetSupportedQuoteCurrencies(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|supported-vs-currencies";
        return await GetFromCacheOrApi(cacheKey, async () => await GetSupportedQuoteCurrenciesInternal(cancellationToken));
    }

    /// <inheritdoc />
    public async Task<string?> GetCoinGeckoIdFromSymbol(string symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|id-from-symbol|{symbol}";
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinGeckoIdFromSymbolInternal(symbol, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<List<Coins>> GetCoinsFromSymbol(string symbol, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|coins-from-symbol|{symbol}";
        return await GetFromCacheOrApi(cacheKey, async () => await GetCoinsFromSymbolInternal(symbol, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<SymbolToCoinGeckoIdsMap> MapRankedSymbolsToCoinGeckoIds(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{_typeName}|symbol-to-ids-map";
        return await GetFromCacheOrApi(cacheKey, async () => await MapRankedSymbolsToCoinGeckoIdsInternal(cancellationToken));
    }
}

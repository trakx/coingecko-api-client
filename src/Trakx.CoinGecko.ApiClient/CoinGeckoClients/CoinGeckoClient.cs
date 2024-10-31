using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Trakx.Common.DateAndTime;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public partial class CoinGeckoClient : ICoinGeckoClient
{
    internal const string MainQuoteCurrency = Constants.Usd;

    private static readonly TimeSpan DefaultCacheLifeSpan = TimeSpan.FromDays(1);
    private static readonly ILogger Logger = LoggerProvider.Create<CoinGeckoClient>();

    private readonly IMemoryCache _memoryCache;
    protected readonly string? _typeName;

    public CoinGeckoClient(
        IMemoryCache memoryCache,
        ICoinsClient coinsClient,
        ISimpleClient simpleClient,
        ISearchClient searchClient,
        IDateTimeProvider dateTimeProvider)
    {
        _memoryCache = memoryCache;
        _coinsClient = coinsClient;
        _simpleClient = simpleClient;
        _searchClient = searchClient;
        _dateTimeProvider = dateTimeProvider;
        _typeName = GetType().Name;
    }

    private async Task<T> GetFromCacheOrApi<T>(string cacheKey, Func<Task<T>> getFromApi)
    {
        var value = await _memoryCache.GetOrCreateAsync<T>(cacheKey, async (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheLifeSpan;
            return await getFromApi();
        });

        return value!;
    }

    internal string BuildCacheKey(params object?[] keyFragments)
    {
        return string.Join('|', keyFragments.Prepend(_typeName));
    }
}

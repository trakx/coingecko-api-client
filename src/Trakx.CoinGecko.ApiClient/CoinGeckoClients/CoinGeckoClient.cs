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

    private readonly IMemoryCache _cache;
    protected readonly string? _typeName;

    public CoinGeckoClient(
        IMemoryCache cache,
        ICoinsClient coinsClient,
        ISimpleClient simpleClient,
        ISearchClient searchClient,
        IDateTimeProvider dateTimeProvider)
    {
        _cache = cache;
        _coinsClient = coinsClient;
        _simpleClient = simpleClient;
        _searchClient = searchClient;
        _dateTimeProvider = dateTimeProvider;
        _typeName = GetType().FullName;
    }

    private async Task<T> GetFromCacheOrApi<T>(string cacheKey, Func<Task<T>> getFromApi)
    {
        var value = await _cache.GetOrCreateAsync<T>(cacheKey, async (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheLifeSpan;
            return await getFromApi();
        });

        return value!;
    }
}

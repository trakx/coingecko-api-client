using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Trakx.Common.ApiClient.Exceptions;
using Trakx.Common.Caching;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public sealed record CachedHttpResponse(HttpStatusCode StatusCode, string Content);

public class CachedHttpClientHandler : DelegatingHandler
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _cacheOptions;

    private static readonly ILogger Logger = LoggerProvider.Create<CachedHttpClientHandler>();

    /// <summary>
    /// A <see cref="DelegatingHandler" /> which caches results for <see cref="CoinGeckoApiConfiguration.CacheDuration"/> time.<br />
    /// The goal is to avoid unnecessary requests to the external CoinGecko API.
    /// </summary>
    public CachedHttpClientHandler(IDistributedCache cache, CoinGeckoApiConfiguration apiConfiguration)
    {
        _cache = cache;

        _cacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = apiConfiguration.CacheDuration
        };
    }

    /// <inheritdoc cref="SendAsyncInternal"/>
    protected override HttpResponseMessage Send(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsync(request, cancellationToken).GetAwaiter().GetResult();
    }

    /// <inheritdoc cref="SendAsyncInternal"/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await SendAsyncInternal(request, cancellationToken);
    }

    /// <summary>
    /// Any time an http client request is invoked,
    /// this method will try to retrieve the result from cache via <see cref="GetRequestFromCacheOrSendAsync"/>
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    internal async Task<HttpResponseMessage> SendAsyncInternal(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        // Data cache is only applicable for GET operations
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        return await GetRequestFromCacheOrSendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Any time an http client request is invoked, we need first check if the result was already cached on previous requests.
    /// If so, we don't need to send a new request to the api.
    /// </summary>
    /// <param name="request">Http request that should be performed</param>
    /// <param name="cancellationToken"></param>
    internal async Task<HttpResponseMessage> GetRequestFromCacheOrSendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cacheKey = await GetCacheKey(request, cancellationToken);

        var cachedResponse = await _cache.GetAsync<CachedHttpResponse>(cacheKey, cancellationToken);
        if (cachedResponse != null)
        {
            Logger.LogDebug("Reusing cached response for {cacheKey}", cacheKey);
            return CreateResponse(cachedResponse.StatusCode, cachedResponse.Content);
        }

        HttpResponseMessage freshResponse;
        try
        {
            freshResponse = await base.SendAsync(request, cancellationToken);
            if (!freshResponse.IsSuccessStatusCode) return freshResponse;
        }
        catch (ApiException exception)
        {
            Logger.LogError(exception, "Failed to get response from {uri}", request.RequestUri?.ToString() ?? "unknown URI");
            return CreateResponse((HttpStatusCode)exception.StatusCode, exception.Message);
        }

        // cache the fresh response
        var freshContent = await freshResponse.Content.ReadAsStringAsync(cancellationToken);
        cachedResponse = new CachedHttpResponse(freshResponse.StatusCode, freshContent);
        await _cache.SetAsync(cacheKey, cachedResponse, _cacheOptions, cancellationToken);

        return CreateResponse(cachedResponse.StatusCode, cachedResponse.Content);
    }

    internal static async Task<string> GetCacheKey(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? payload
            = request.Content == null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return $"{request.RequestUri}|{payload}";
    }

    internal static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content)
    {
        return new() { StatusCode = statusCode, Content = new StringContent(content) };
    }
}

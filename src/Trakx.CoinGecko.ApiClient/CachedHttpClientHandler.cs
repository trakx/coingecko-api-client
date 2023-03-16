using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Trakx.CoinGecko.ApiClient;

public interface ISemaphore : IDisposable
{
    Task WaitAsync(CancellationToken cancellationToken);
    int Release(int count);
}

public class Semaphore : ISemaphore
{
    private readonly SemaphoreSlim _semaphoreImplementation;

    public Semaphore(SemaphoreSlim semaphoreImplementation)
    {
        _semaphoreImplementation = semaphoreImplementation;
    }

    public void Dispose()
    {
        _semaphoreImplementation.Dispose();
    }

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _semaphoreImplementation.WaitAsync(cancellationToken);
    }

    public int Release(int count)
    {
        return _semaphoreImplementation.Release(count);
    }
}

public class CachedHttpClientHandler : DelegatingHandler
{
    private static readonly ILogger Logger =
        Log.Logger.ForContext(MethodBase.GetCurrentMethod()!.DeclaringType!);

    private readonly IMemoryCache _cache;
    private readonly CoinGeckoApiConfiguration _apiConfiguration;
    private readonly ISemaphore _semaphore;
    private readonly int _millisecondsDelay;

    /// <summary>
    /// An <see cref="DelegatingHandler"></see> with a throttle to limit the maximum rate at which queries are sent
    /// and also periodically caching the results being retrieved to avoid unnecessary requests to coin gecko external api.
    /// </summary>
    public CachedHttpClientHandler(IMemoryCache cache,
        ISemaphore semaphore,
        CoinGeckoApiConfiguration apiConfiguration)
    {
        _cache = cache;
        _apiConfiguration = apiConfiguration;
        _millisecondsDelay = apiConfiguration.InitialRetryDelayInMilliseconds ?? 100;
        _semaphore = semaphore;
    }

    /// <summary>
    /// Any time an http client request is invoked this method will first check
    /// if the throttle request is inside the maximum limit range defined in <see cref="CoinGeckoApiConfiguration.InitialRetryDelayInMilliseconds"/>;
    /// after that tries to retrieve the result from cache via <see cref="TryGetOrSetRequestFromCache"/>
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        return await TryGetOrSetRequestFromCache(request, cancellationToken)
            .ConfigureAwait(false);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        return TryGetOrSetRequestFromCache(request, cancellationToken)
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Any time an http client request is invoked, we need first check if the result was already cached on previous requests.
    /// If so, we don't need to send a new request to the api.
    /// </summary>
    /// <param name="request">Http request that should be performed</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<HttpResponseMessage> TryGetOrSetRequestFromCache(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Data cache is only applicable for GET operations
        if (request.Method != HttpMethod.Get)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                return await base.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                await Task.Delay(_millisecondsDelay, cancellationToken).ConfigureAwait(false);
                _semaphore.Release(1);
            }
        }

        var key = $"[{request.Method}]{request.RequestUri}|{request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult()}";

        var (message, cachedContent) = await _cache.GetOrCreateAsync(key, async e =>
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                var response = await base.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                e.AbsoluteExpirationRelativeToNow = response.IsSuccessStatusCode
                    ? TimeSpan.FromSeconds(_apiConfiguration.CacheDurationInSeconds ?? 10)
                    : TimeSpan.FromTicks(1);

                return (response, content);
            }
            catch (ApiException exception)
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(1);
                if(exception.StatusCode == (int)HttpStatusCode.TooManyRequests
                   && exception.Headers.TryGetValue("Retry-After", out var value))
                {
                    var millisecondsDelay = int.Parse(value?.First() ?? "0");
                    await Task.Delay(millisecondsDelay, cancellationToken);
                }

                Logger.Warning(exception, "Failed to get response from {uri}", request.RequestUri?.ToString() ?? "unknown URI");
                return (new HttpResponseMessage((HttpStatusCode)exception.StatusCode), exception.Message);
            }
            finally
            {
                await Task.Delay(_millisecondsDelay, cancellationToken).ConfigureAwait(false);
                _semaphore.Release(1);
            }
        }).ConfigureAwait(false);

        return new HttpResponseMessage(message.StatusCode){Content = new StringContent(cachedContent)};
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore.Dispose();
        }

        base.Dispose(disposing);
    }

}

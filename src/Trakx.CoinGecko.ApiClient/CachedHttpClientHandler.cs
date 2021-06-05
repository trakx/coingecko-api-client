using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Trakx.CoinGecko.ApiClient
{
    public class CachedHttpClientHandler : DelegatingHandler
    {
        private static readonly ILogger Logger =
            Log.Logger.ForContext(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IMemoryCache _cache;
        private readonly CoinGeckoApiConfiguration _apiConfiguration;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _millisecondsDelay;

        /// <summary>
        /// An <see cref="DelegatingHandler"></see> with a throttle to limit the maximum rate at which queries are sent
        /// and also periodically caching the results being retrieved to avoid unnecessary requests to coin gecko external api.
        /// </summary>
        public CachedHttpClientHandler(IMemoryCache cache,
            CoinGeckoApiConfiguration apiConfiguration)
        {
            _cache = cache;
            _apiConfiguration = apiConfiguration;
            _millisecondsDelay = apiConfiguration.InitialRetryDelayInMilliseconds ?? 100;
            _semaphore = new SemaphoreSlim(1, 1);
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

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await TryGetOrSetRequestFromCache(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await Task.Delay(_millisecondsDelay, cancellationToken).ConfigureAwait(false);
                _semaphore.Release(1);
            }
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
                return await base.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
            }

            var key = $"[{request.Method}]{request.RequestUri}|{request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult()}";

            var cachedResponse = await _cache.GetOrCreateAsync(key, async e =>
            {
                try
                {
                    var response = await base.SendAsync(request, cancellationToken)
                        .ConfigureAwait(false);
                    
                    e.Value = response;
                    e.AbsoluteExpirationRelativeToNow = response.IsSuccessStatusCode 
                        ? TimeSpan.FromSeconds(_apiConfiguration.CacheDurationInSeconds ?? 10)
                        : TimeSpan.FromTicks(1);

                    return response;
                }
                catch (Exception exception)
                {
                    e.AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks(1);
                    Logger.Warning(exception, "Failed to get response from {uri}", request.RequestUri?.ToString() ?? "unknown URI");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }).ConfigureAwait(false);

            return cachedResponse;
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
}

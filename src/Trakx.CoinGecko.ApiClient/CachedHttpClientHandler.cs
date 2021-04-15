using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Trakx.CoinGecko.ApiClient
{
    public class CachedHttpClientHandler : DelegatingHandler
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        private readonly CoinGeckoApiConfiguration _apiConfiguration;
        private static readonly ConcurrentDictionary<string, object> Requests = new ConcurrentDictionary<string, object>();
        private readonly SemaphoreSlim _semaphore;
        private readonly int _millisecondsDelay;

        /// <summary>
        /// An <see cref="DelegatingHandler"></see> with a throttle to limit the maximum rate at which queries are sent
        /// and also periodically caching the results being retrieved to avoid unnecessary requests to coin gecko external api.
        /// </summary>
        public CachedHttpClientHandler(IMemoryCache cache, ILogger logger, 
            CoinGeckoApiConfiguration apiConfiguration)
        {
            _logger = logger;
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
                return TryGetOrSetRequestFromCache(request, cancellationToken);
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
        private HttpResponseMessage TryGetOrSetRequestFromCache(HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            // Data cache is only applicable for GET operations
            if (request.Method != HttpMethod.Get)
            {
                return base.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            }

            string content;
            var key = $"[{request.Method}]{request.RequestUri}";
            if (!Requests.ContainsKey(key))
            {
                Requests[key] = new object();
            }

            lock (Requests[key])
            {
                if (_cache.TryGetValue(key, out content))
                {
                    _logger.Debug($"Loaded {request.Method} http response to {request.RequestUri} from CACHE.");
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(content)
                    };
                }

                var response = base.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                content = response.Content.ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                var cacheExpirationOptions = new MemoryCacheEntryOptions
                {
                    // Set to use 'AbsoluteExpiration' as 'SlidingExpiration' was not working as expected (i.e. set to 30 seconds and it never got expired)
                    AbsoluteExpiration = DateTime.UtcNow.AddSeconds(_apiConfiguration.CacheDurationInSeconds ?? 10),
                    Priority = CacheItemPriority.Normal
                };
                _logger.Debug($"Cached {request.Method} http response to {request.RequestUri}.");
                _cache.Set(key, content, cacheExpirationOptions);

                return response;
            }
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

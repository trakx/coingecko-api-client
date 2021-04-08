using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;

namespace Trakx.Common.Sources.CoinGecko
{
    public class ThrottledHttpClientHandler : HttpClientHandler
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly int _millisecondsDelay;

        /// <summary>
        /// An <see cref="HttpClientHandler"></see> with a throttle to limit the maximum rate at which queries are sent.
        /// </summary>
        /// <param name="millisecondsDelay">The number of milliseconds to wait between calls to the base <see cref="HttpClientHandler.SendAsync"></see> method.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="millisecondsDelay">millisecondsDelay</paramref> argument is less than or equal to 0.</exception>
        public ThrottledHttpClientHandler(int millisecondsDelay)
        {
            if (millisecondsDelay <= 0) { throw new ArgumentOutOfRangeException(nameof(millisecondsDelay)); }

            _millisecondsDelay = millisecondsDelay;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request, nameof(request));

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                await Task.Delay(_millisecondsDelay, cancellationToken).ConfigureAwait(false);
                _semaphore.Release(1);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
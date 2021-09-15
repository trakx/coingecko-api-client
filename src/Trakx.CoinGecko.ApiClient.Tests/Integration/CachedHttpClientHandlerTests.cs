using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class CachedHttpClientHandlerTests
    {
        private readonly ICoinGeckoClient _client;
        private readonly int _throttleDelay;

        public CachedHttpClientHandlerTests(ITestOutputHelper output)
        {
            var serviceCollection = new ServiceCollection();
            _throttleDelay = 100;
            var config = new CoinGeckoApiConfiguration
            {
                BaseUrl = CoinGeckoApiFixture.CoinGeckoProBaseUrl,
                ThrottleDelayPerSecond = _throttleDelay,
                ApiKey = CoinGeckoApiFixture.CoinGeckoApiKey
            };
            serviceCollection.AddCoinGeckoClient(config);
            serviceCollection.AddMemoryCache();
            serviceCollection.AddSingleton(config);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            _client = serviceProvider.GetService<ICoinGeckoClient>()!;
        }

        [Fact]
        public async Task Registering_Coingecko_clients_should_use_throttled_message_handler()
        {
            var allCoins = await _client.GetCoinList().ConfigureAwait(false);
            var ids = allCoins.Take(15).Select(c => c.Id);
            var latestPrices = ids
                .Select(async id => await _client.GetLatestPrice(id, "usd"));
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await Task.WhenAll(latestPrices.ToArray());
            stopWatch.Stop();

            stopWatch.Elapsed.Should().BeGreaterOrEqualTo(TimeSpan.FromMilliseconds(_throttleDelay * 15),
                "ThrottleHttpHandler imposes a _throttleDelay delay between each call.");
        }
    }
}
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Trakx.CoinGecko.ApiClient;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.Common.Tests.Integration.Sources.CoinGecko
{
    public class ThrottledClientTests
    {
        private readonly ICoinGeckoClient _client;
        private readonly int _throttleDelay;

        public ThrottledClientTests(ITestOutputHelper output)
        {
            var serviceCollection = new ServiceCollection();
            _throttleDelay = 100;
            var config = new CoinGeckoApiConfiguration { ThrottleDelayPerSecond = _throttleDelay };
            serviceCollection.AddCoinGeckoClient(config);
            serviceCollection.AddMemoryCache();
            serviceCollection.AddSingleton(output.ToLogger<CoinGeckoClient>());
            var serviceProvider = serviceCollection.BuildServiceProvider();

            _client = serviceProvider.GetService<ICoinGeckoClient>()!;
        }

        [Fact]
        public async Task Registering_Coingecko_clients_should_use_throttled_message_handler()
        {
            var latestPrices = Enumerable.Repeat("fetch", 15)
                .Select(async _ => await _client.GetLatestPrice("bitcoin", "usd"));
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await Task.WhenAll(latestPrices.ToArray());
            stopWatch.Stop();

            stopWatch.Elapsed.Should().BeGreaterOrEqualTo(TimeSpan.FromMilliseconds(_throttleDelay * 15),
                "ThrottleHttpHandler imposes a _throttleDelay delay between each call.");
        }
    }
}
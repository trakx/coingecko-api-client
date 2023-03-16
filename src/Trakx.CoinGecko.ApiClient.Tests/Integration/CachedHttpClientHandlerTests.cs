using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class CachedHttpClientHandlerTests : IDisposable
{
    private readonly CoinGeckoApiConfiguration _config;
    private readonly ServiceProvider _serviceProvider;
    private readonly ICoinGeckoClient _client;

    public CachedHttpClientHandlerTests(ITestOutputHelper output)
    {
        _config = new CoinGeckoApiConfiguration
        {
            BaseUrl = CoinGeckoApiFixture.CoinGeckoBaseUrl,
            ThrottleDelayPerSecond = 100
        };

        _serviceProvider = BuildServiceProvider();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.TestOutput(output)
            .CreateLogger()
            .ForContext<CachedHttpClientHandlerTests>();

        _client = _serviceProvider.GetService<ICoinGeckoClient>()!;
    }

    private ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddCoinGeckoClient(_config);
        serviceCollection.AddMemoryCache();
        serviceCollection.AddSingleton(_config);
        return serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Registering_Coingecko_clients_should_use_throttled_message_handler()
    {
        const int howManyCoinsInTest = 3;

        var allCoins = await _client.GetCoinList().ConfigureAwait(false);
        var ids = allCoins.Where(c => c.Id != "0vix-protocol") //this coin is not quoted in USD
            .Take(howManyCoinsInTest).Select(c => c.Id);
        var latestPrices = ids
            .Select(async id => await _client.GetLatestPrice(id, "usd"));
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        await Task.WhenAll(latestPrices.ToArray());
        stopWatch.Stop();

        var expectedDelay = TimeSpan.FromMilliseconds(howManyCoinsInTest * (double)_config.ThrottleDelayPerSecond!);
        stopWatch.Elapsed.Should().BeGreaterOrEqualTo(expectedDelay,
            "ThrottleHttpHandler imposes a _throttleDelay delay between each call.");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _serviceProvider.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

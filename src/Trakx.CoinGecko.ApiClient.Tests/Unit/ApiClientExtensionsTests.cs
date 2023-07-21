using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Trakx.CoinGecko.ApiClient.Tests.Integration;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public class ApiClientExtensionsTests
{
    [Fact]
    public void AddCoinGeckoClient_sets_expected_base_url_and_timeout()
    {
        var configuration = new CoinGeckoApiConfiguration
        {
            BaseUrl = CoinGeckoApiFixture.FreeBaseUrl,
            Timeout = TimeSpan.FromSeconds(123)
        };

        var configurator = new ClientConfigurator(configuration);
        var delays = configuration.InitialRetryDelay.AsSingletonList();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddCoinGeckoClient(configuration);

        // custom test client
        serviceCollection.AddHttpClientForCoinGeckoClient<IExtensionsTestClient, ExtensionsTestClient>(configurator, delays);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var testClient = serviceProvider.GetRequiredService<IExtensionsTestClient>();

        testClient.HttpClient.Timeout.Should().Be(configuration.Timeout);
        testClient.Url.Should().Be(configuration.BaseUrl);
    }
}

internal interface IExtensionsTestClient
{
    HttpClient HttpClient { get; }
    Uri Url { get; }
}

internal class ExtensionsTestClient : AuthorisedClient, IExtensionsTestClient
{
    public HttpClient HttpClient { get; }

    public Uri Url => new(base.BaseUrl);

    public ExtensionsTestClient(ClientConfigurator configuration, HttpClient httpClient)
        : base(configuration)
    {
        HttpClient = httpClient;
    }
}
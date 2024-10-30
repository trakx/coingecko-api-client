using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Trakx.Common.Infrastructure.Caching;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public class DependencyInjectionTests
{
    private readonly CoinGeckoApiConfiguration _coinGeckoApiConfiguration;

    private readonly ServiceCollection _serviceCollection;

    public DependencyInjectionTests()
    {
        var redisCacheConfiguration = new RedisCacheConfiguration();

        _coinGeckoApiConfiguration = new CoinGeckoApiConfiguration
        {
            BaseUrl = Constants.PublicBaseUrl
        };

        _serviceCollection = new ServiceCollection();
        _serviceCollection.AddCoinGeckoClient(_coinGeckoApiConfiguration, redisCacheConfiguration);
    }

    [Fact]
    public void AddCoinGeckoClient_adds_expected_generated_clients()
    {
        var clientType = typeof(AuthorisedClient);
        var implementations = clientType.Assembly.GetImplementationsOf(clientType);

        foreach (var implementation in implementations)
        {
            var interfaceType = implementation.GetInterfaces().FirstOrDefault();
            interfaceType.Should().NotBeNull("client {0} should implement an I{0} interface", implementation.Name);

            _serviceCollection.ExpectService(interfaceType!);
        }
    }

    [Fact]
    public void AddCoinGeckoClient_sets_expected_base_url_and_timeout()
    {
        var configurator = new ClientConfigurator(_coinGeckoApiConfiguration);
        var delays = _coinGeckoApiConfiguration.InitialRetryDelay.AsSingletonList();

        // custom test client
        _serviceCollection.AddHttpClientForCoinGeckoClient<IExtensionsTestClient, ExtensionsTestClient>(configurator, delays);

        var serviceProvider = _serviceCollection.BuildServiceProvider();

        var testClient = serviceProvider.GetRequiredService<IExtensionsTestClient>();

        testClient.HttpClient.Timeout.Should().Be(_coinGeckoApiConfiguration.Timeout);
        testClient.Url.Should().Be(_coinGeckoApiConfiguration.BaseUrl);
    }
}

public interface IExtensionsTestClient
{
    HttpClient HttpClient { get; }
    Uri Url { get; }
}

public class ExtensionsTestClient : AuthorisedClient, IExtensionsTestClient
{
    public HttpClient HttpClient { get; }

    public Uri Url => new(base.BaseUrl);

    public ExtensionsTestClient(ClientConfigurator configuration, HttpClient httpClient)
        : base(configuration)
    {
        HttpClient = httpClient;
    }
}
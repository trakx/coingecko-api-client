using Trakx.Common.Configuration;
using Trakx.Common.Testing.Configuration;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public class AuthorisedClientTests
{
    [Fact]
    public void BaseUrl_ends_with_slash()
    {
        var configurationSource = JsonConfigurationHelper.BuildFromJson($$"""
            {
                "CoinGeckoApiConfiguration":
                {
                    "BaseUrl": "http://no.slash.at.end"
                }
            }
            """);

        var config = configurationSource.GetConfiguration<CoinGeckoApiConfiguration>();
        var configurator = new ClientConfigurator(config);
        var client = new TestClient(configurator);

        client.BaseUrl[^1].Should().Be('/');
    }
}

internal class TestClient : AuthorisedClient
{
    public TestClient(ClientConfigurator configurator) : base(configurator)
    {
    }
}

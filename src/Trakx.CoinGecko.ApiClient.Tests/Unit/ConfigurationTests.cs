using Trakx.CoinGecko.ApiClient.Tests.Integration;
using Trakx.Common.Configuration;
using Trakx.Common.Testing.Configuration;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public class ConfigurationTests
{
    [Fact]
    public void Uri_is_loaded_from_string()
    {
        var configurationSource = JsonConfigurationHelper.BuildFromJson($$"""
            {
                "CoinGeckoApiConfiguration":
                {
                    "BaseUrl": "{{CoinGeckoApiFixture.FreeBaseUrl.OriginalString}}"
                }
            }
            """);

        var configurationObject = configurationSource.GetConfiguration<CoinGeckoApiConfiguration>();
        configurationObject.BaseUrl.Should().Be(CoinGeckoApiFixture.FreeBaseUrl);
    }
}
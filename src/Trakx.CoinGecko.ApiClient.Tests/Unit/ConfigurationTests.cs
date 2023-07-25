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
                    "BaseUrl": "{{Constants.PublicBaseUrl.OriginalString}}"
                }
            }
            """);

        var configurationObject = configurationSource.GetConfiguration<CoinGeckoApiConfiguration>();
        configurationObject.BaseUrl.Should().Be(Constants.PublicBaseUrl);
    }
}
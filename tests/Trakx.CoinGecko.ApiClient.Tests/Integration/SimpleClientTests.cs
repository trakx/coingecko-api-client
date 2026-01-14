using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class SimpleClientTests : CoinGeckoClientTestBase
{
    private readonly ISimpleClient _simpleClient;

    public SimpleClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
        : base(apiFixture, output)
    {
        _simpleClient = ServiceProvider.GetRequiredService<ISimpleClient>();
    }

    [Theory]
    //[ClassData(typeof(CoinGeckoIdsTestData))]
    [InlineData("bitcoin")]
    [InlineData("ethereum")]
    [InlineData("cardano")]
    [InlineData("binancecoin")]
    public async Task PriceAsync_should_return_price_when_passing_valid_symbol(string id)
    {
        var price = await _simpleClient.PriceAsync(id, Constants.Usd).ConfigureAwait(true);
        price.StatusCode.Should().Be((int)HttpStatusCode.OK);
        price.Content.Keys.Should().Contain(id);
        price.Content[id].Keys.Should().Contain(Constants.Usd);
        price.Content[id][Constants.Usd].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Supported_vs_currencies()
    {
        var currencies = await _simpleClient.Supported_vs_currenciesAsync();
        currencies.StatusCode.Should().Be((int)HttpStatusCode.OK);
        currencies.Content.Should().NotBeEmpty();
    }
}
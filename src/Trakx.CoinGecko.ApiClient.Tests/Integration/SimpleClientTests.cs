using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

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
        var price = await _simpleClient.PriceAsync(id, "usd").ConfigureAwait(true);
        price.StatusCode.Should().Be((int)HttpStatusCode.OK);
        price.Result.Keys.Should().Contain(id);
        price.Result[id].Keys.Should().Contain(Constants.Usd);
        price.Result[id][Constants.Usd].Should().BeGreaterThan(0);
    }

}
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class SimpleClientTests : CoinGeckoClientTestBase
    {
        private readonly ISimpleClient _simpleClient;

        public SimpleClientTests(CoinGeckoApiFixture apiFixture) 
            : base(apiFixture)
        {
            _simpleClient = ServiceProvider.GetRequiredService<ISimpleClient>();
        }

        [Fact]
        public async Task PriceAsync_should_return_price_when_passing_valid_symbol()
        {
            var price = await _simpleClient.PriceAsync("bitcoin", "usd");
            price.StatusCode.Should().Be((int)HttpStatusCode.OK);
            price.Result.Keys.Should().Contain(Constants.Bitcoin);
            price.Result[Constants.Bitcoin].Keys.Should().Contain(Constants.Usd);
            price.Result[Constants.Bitcoin][Constants.Usd].Should().BeGreaterThan(0);
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

    }
}

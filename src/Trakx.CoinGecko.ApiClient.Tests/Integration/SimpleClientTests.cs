using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class SimpleClientTests : CoinGeckoClientTestsBase
    {
        private readonly ISimpleClient _simpleClient;
        private readonly ICoinsClient _coinsClient;

        public SimpleClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output) : base(apiFixture, output)
        {
            _coinsClient = ServiceProvider.GetRequiredService<ICoinsClient>();
            _simpleClient = ServiceProvider.GetRequiredService<ISimpleClient>();
        }

        [Fact]
        public async Task GetTicker_should_return_tickers_for_all_exchanges()
        {
            var result = await _coinsClient.ListAllAsync();
            var list = result.Result;
            list.Should().NotBeEmpty();
        }

    }
}

using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class CoinsClientTests : CoinGeckoClientTestBase
    {

        private readonly ICoinsClient _coinsClient;

        public CoinsClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output) : base(apiFixture, output)
        {
            _coinsClient = ServiceProvider.GetRequiredService<ICoinsClient>();
        }

        [Fact]
        public async Task ListAllAsync_should_all_coins_from_the_coingecko()
        {
            var result = await _coinsClient.ListAllAsync();
            var list = result.Result;
            list.Should().NotBeEmpty();
            EnsureAllJsonElementsWereMapped(list);
        }

        public string GetResource(string path)
        {
            using (var stream = this.GetType().Assembly.
                GetManifestResourceStream("Trakx.CoinGecko.ApiClient.Tests." + path))
            {
                using (var sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        [Fact]
        public async Task CoinsAsync_should_return_a_valid_coindata_when_passing_a_valid_id()
        {
            var result = await _coinsClient.CoinsAsync("bitconnect", "false");
            var list = result.Result;
            EnsureAllJsonElementsWereMapped(list);
        }

        [Fact]
        public async Task HistoryAsync_should_historical_data_when_passing_valid_id()
        {
            var history = await _coinsClient.HistoryAsync("bitconnect", "30-01-2021", "true");
            history.StatusCode.Should().Be((int)HttpStatusCode.OK);
            history.Result.Id.Should().Be("bitconnect");
            history.Result.Symbol.Should().Be("bcc");
            history.Result.Name.Should().Be("Bitconnect");
            history.Result.Image.Thumb.Should().NotBeEmpty();
            history.Result.Image.Small.Should().NotBeEmpty();
            history.Result.Market_data.Current_price.Should().NotBeEmpty();
            history.Result.Market_data.Market_cap.Should().NotBeEmpty();
            EnsureAllJsonElementsWereMapped(history);
        }

    }
}

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class CoinsClientTests : CoinGeckoClientTestBase
    {

        private readonly ICoinsClient _coinsClient;

        public CoinsClientTests(CoinGeckoApiFixture apiFixture) 
            : base(apiFixture)
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

        [Fact]
        public async Task CoinsAsync_should_return_a_valid_coindata_when_passing_a_valid_id()
        {
            var result = await _coinsClient.CoinsAsync(Constants.BitConnect, "false");
            var list = result.Result;
            EnsureAllJsonElementsWereMapped(list);
        }

        [Fact]
        public async Task HistoryAsync_should_historical_data_when_passing_valid_id()
        {
            var history = await _coinsClient.HistoryAsync(Constants.BitConnect, "30-01-2021", "true");
            history.StatusCode.Should().Be((int)HttpStatusCode.OK);
            history.Result.Id.Should().Be(Constants.BitConnect);
            history.Result.Symbol.Should().Be(Constants.Bcc);
            history.Result.Name.Should().Be("Bitconnect");
            history.Result.Image.Thumb.Should().NotBeEmpty();
            history.Result.Image.Small.Should().NotBeEmpty();
            history.Result.Market_data.Current_price.Should().NotBeEmpty();
            history.Result.Market_data.Market_cap.Should().NotBeEmpty();
            EnsureAllJsonElementsWereMapped(history);
        }

        [Fact]
        public async Task RangeAsync_should_return_historical_data_for_a_range_of_dates()
        {
            var start = DateTimeOffset.Parse("2020-12-01");
            var end = DateTimeOffset.Parse("2020-12-31");var range = await _coinsClient
                .RangeAsync("ethereum", "usd", start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), CancellationToken.None)
                .ConfigureAwait(false);
            
            range.StatusCode.Should().Be((int)HttpStatusCode.OK);
            range.Result.Prices.Count.Should().BeGreaterThan(30);
            range.Result.Market_caps.Count.Should().Be(range.Result.Prices.Count);
            range.Result.Total_volumes.Count.Should().Be(range.Result.Prices.Count);

            DateTimeOffset.FromUnixTimeMilliseconds((long) range.Result.Prices.First()[0]).Should().BeCloseTo(start, TimeSpan.FromDays(1));
            range.Result.Prices.First()[1].Should().BeApproximately(613d, 10d);
            DateTimeOffset.FromUnixTimeMilliseconds((long) range.Result.Prices.Last()[0]).Should().BeCloseTo(end, TimeSpan.FromDays(1));
            range.Result.Prices.Last()[1].Should().BeApproximately(746d, 10d);
            EnsureAllJsonElementsWereMapped(range);
        }
    }
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class CoinGeckoClientTests : CoinGeckoClientTestBase
    {

        private readonly ICoinGeckoClient _coinsClient;
        private readonly string _coinGeckoId;
        private readonly string _quoteCurrencyId;
        private readonly DateTime _asOf;

        public CoinGeckoClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
            : base(apiFixture, output)
        {
            _coinsClient = ServiceProvider.GetRequiredService<ICoinGeckoClient>();
            
            _coinGeckoId = Constants.BitConnect;
            _quoteCurrencyId = Constants.UsdCoin;
            _asOf = DateTime.Today.AddDays(-5);
        }

        [Fact]
        public async Task GetLatestPrice_should_return_valid_price_when_passing_valid_id()
        {
            var result = await _coinsClient.GetLatestPrice(_coinGeckoId, _quoteCurrencyId);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPriceAsOfFromId_should_return_valid_price_when_passing_valid_id()
        {
            var result = await _coinsClient.GetPriceAsOfFromId(_coinGeckoId, _asOf, _quoteCurrencyId);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMarketDataAsOfFromId_should_return_valid_data_when_passing_valid_id()
        {
            var result = await _coinsClient.GetMarketDataAsOfFromId(_coinGeckoId, _asOf, _quoteCurrencyId);
            result.AsOf.Should().NotBeNull();
            result.CoinId.Should().Be(_coinGeckoId);
            result.CoinSymbol.Should().NotBeEmpty();
            result.MarketCap.Should().NotBeNull();
            result.Price.Should().BeGreaterThan(0);
            result.Volume.Should().BeGreaterThan(0);
            result.QuoteCurrency.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetCoinGeckoIdFromSymbol_should_return_valid_data_when_passing_valid_id()
        {
            var result = await _coinsClient.GetCoinGeckoIdFromSymbol("btc");
            result.Should().Be(Constants.Bitcoin);
        }

        [Fact]
        public async Task GetCoinList_should_return_the_full_list_when_passing_no_arguments()
        {
            var result = await _coinsClient.GetCoinList();
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetAllPrices_should_return_a_valid_list_of_prices_when_passing_valid_ids_and_currencies()
        {
            var result = await _coinsClient.GetAllPrices(
                new[] { _coinGeckoId, Constants.Bitcoin },
                new[] { Constants.Usd });
            result.Keys.Should().Contain(_coinGeckoId);
            result.Keys.Should().Contain(Constants.Bitcoin);
            foreach (var prices in result.Values)
            {
                prices.Keys.Should().OnlyContain(f => f == Constants.Usd);
                prices.Values.Should().OnlyContain(f => f > 0);
            }
        }

    }
}

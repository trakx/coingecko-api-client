using System;
using System.Threading;
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

            _coinGeckoId = Constants.Coins.Bitcoin;
            _quoteCurrencyId = Constants.Coins.UsdCoin;
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
            result.Should().Be(Constants.Coins.Bitcoin);
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
                new[] { _coinGeckoId, Constants.Coins.Aave },
                new[] { Constants.Currencies.Usd });
            result.Keys.Should().Contain(_coinGeckoId);
            result.Keys.Should().Contain(Constants.Coins.Aave);
            foreach (var prices in result.Values)
            {
                prices.Keys.Should().OnlyContain(f => f == Constants.Currencies.Usd);
                prices.Values.Should().OnlyContain(f => f > 0);
            }
        }

        [Fact]
        public async Task GetAllPricesExtended_should_return_marketCap_and_dailyVolume()
        {
            var result = await _coinsClient.GetAllPricesExtended(
                new[] { Constants.Coins.Aave, Constants.Coins.Bitcoin },
                new[] { Constants.Currencies.Usd, Constants.Currencies.Eth },
                includeMarketCap: true,
                include24HrVol: true);

            result.Should().Contain(p => p.CoinGeckoId == Constants.Coins.Aave && p.Currency == Constants.Currencies.Usd);
            result.Should().Contain(p => p.CoinGeckoId == Constants.Coins.Bitcoin && p.Currency == Constants.Currencies.Usd);
            result.Should().Contain(p => p.CoinGeckoId == Constants.Coins.Aave && p.Currency == Constants.Currencies.Eth);
            result.Should().Contain(p => p.CoinGeckoId == Constants.Coins.Bitcoin && p.Currency == Constants.Currencies.Eth);
            result.Should().OnlyContain(p => p.MarketCap > 0 && p.DailyVolume > 0);
            result.Should().HaveCount(4);
        }

        [Fact]
        public async Task GetMarketData_should_return_valid_data_when_passing_valid_id()
        {
            var currencyId = Constants.Currencies.Usd;
            int daysCount = 2;
            var result = await _coinsClient.GetMarketData(_coinGeckoId, currencyId, daysCount, CancellationToken.None);
            result.Should().HaveCount(daysCount + 1);
            foreach (var item in result)
            {
                item.Value.AsOf.Should().NotBeNull();
                item.Value.CoinId.Should().Be(_coinGeckoId);
                item.Value.MarketCap.Should().NotBeNull();
                item.Value.Price.Should().BeGreaterThan(0);
                item.Value.Volume.Should().BeGreaterThan(0);
                item.Value.QuoteCurrency.Should().Be(currencyId);
            }
        }
    }
}

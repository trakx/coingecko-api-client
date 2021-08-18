using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class CoinGeckoClientTests : CoinGeckoClientTestBase
    {

        private readonly ICoinGeckoClient _coinsClient;
        private readonly string _quoteCurrencyId;
        private readonly DateTime _asOf;

        public CoinGeckoClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
            : base(apiFixture, output)
        {
            _coinsClient = ServiceProvider.GetRequiredService<ICoinGeckoClient>();

            _quoteCurrencyId = Constants.UsdCoin;
            _asOf = DateTime.Today.AddDays(-5);
        }

        [Theory]
        [ClassData(typeof(CoinGeckoIdsTestData))]
        public async Task GetLatestPrice_should_return_valid_price_when_passing_valid_id(string id)
        {
            var result = await _coinsClient.GetLatestPrice(id, Constants.Usd);
            result.Should().NotBeNull();
        }

        [Theory]
        [ClassData(typeof(CoinGeckoIdsTestData))]
        public async Task GetPriceAsOfFromId_should_return_valid_price_when_passing_valid_id(string id)
        {
            var result = await _coinsClient.GetPriceAsOfFromId(id, _asOf, _quoteCurrencyId);
            result.Should().NotBeNull();
        }

        [Theory]
        [ClassData(typeof(CoinGeckoIdsTestData))]
        public async Task GetMarketDataAsOfFromId_should_return_valid_data_when_passing_valid_id(string id)
        {
            var result = await _coinsClient.GetMarketDataAsOfFromId(id, _asOf, _quoteCurrencyId);
            result!.AsOf.Should().NotBeNull();
            result.CoinId.Should().Be(id);
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
            result.Should().Be("bitcoin");
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
            var ids = GetCoinIds();

            var result = await _coinsClient.GetAllPrices(
                ids,
                new[] { Constants.Usd });
            result.Keys.Should().Contain(ids);
            foreach (var prices in result.Values)
            {
                prices.Keys.Should().OnlyContain(f => f == Constants.Usd);
                prices.Values.Should().OnlyContain(f => f > 0);
            }
        }

        private static string[] GetCoinIds()
        {
            var ids = new CoinGeckoIdsTestData()
                .SelectMany(s => s!)
                .Select(s => s.ToString()!)
                .ToArray();
            return ids;
        }

        [Fact]
        public async Task GetAllPricesExtended_should_return_marketCap_and_dailyVolume()
        {
            var coinIds = GetCoinIds();
            var quoteCurrencies = new[] { Constants.Usd, "eth"};
            var result = await _coinsClient.GetAllPricesExtended(
                coinIds,
                quoteCurrencies,
                includeMarketCap: true,
                include24HrVol: true);

            foreach (var coinId in coinIds)
            foreach (var quoteCurrency in quoteCurrencies)
            {
                result.Should().Contain(p => p.CoinGeckoId == coinId && p.Currency == quoteCurrency);
                result.Should().OnlyContain(p => p.MarketCap > 0 && p.DailyVolume > 0);
            }

            result.Should().HaveCount(coinIds.Length * quoteCurrencies.Length);
        }
    }
}

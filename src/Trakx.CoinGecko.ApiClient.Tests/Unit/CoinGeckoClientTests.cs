using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog;
using Trakx.Utils.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit
{
    public class CoinGeckoClientTests
    {

        private readonly ICoinGeckoClient _coinGeckoClient;
        private readonly IClientFactory _clientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger _logger;
        private readonly ISimpleClient _simpleClient;
        private readonly ICoinsClient _coinsClient;
        private readonly MockCreator _mockCreator;

        public CoinGeckoClientTests(ITestOutputHelper output)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddMemoryCache();
            var build = serviceCollection.BuildServiceProvider();
            _memoryCache = build.GetService<IMemoryCache>();

            _clientFactory = Substitute.For<IClientFactory>();
            _simpleClient = Substitute.For<ISimpleClient>();
            _coinsClient = Substitute.For<ICoinsClient>();
            _clientFactory.CreateCoinsClient().Returns(_coinsClient);
            _clientFactory.CreateSimpleClient().Returns(_simpleClient);
            _logger = Substitute.For<ILogger>();
            _mockCreator = new MockCreator(output);

            _coinGeckoClient = new CoinGeckoClient(_clientFactory, _memoryCache, _logger);
        }

        [Fact]
        public async Task GetLatestPrice_should_return_valid_price_when_passing_valid_id()
        {
            var id = _mockCreator.GetRandomString(10);
            var currency = _mockCreator.GetRandomString(5);
            var coinPrice = _mockCreator.GetRandomPrice();
            var currencyPrice = _mockCreator.GetRandomPrice();
            ConfigurePriceAsync(id, currency, coinPrice, currencyPrice);
            var result = await _coinGeckoClient.GetLatestPrice(id, currency);
            result.Should().Be(coinPrice / currencyPrice);
        }

        [Fact]
        public async Task GetPriceAsOfFromId_should_return_valid_price_when_passing_valid_id()
        {
            var asOf = _mockCreator.GetRandomUtcDateTime();

            var coin = _mockCreator.GetRandomString(10);
            var coinPrice = _mockCreator.GetRandomPrice();
            ConfigureHistoryAsync(coin, asOf, coinPrice, 1m);

            var currency = _mockCreator.GetRandomString(10);
            var currencyPrice = _mockCreator.GetRandomPrice();
            ConfigureHistoryAsync(currency, asOf, currencyPrice, 1m);

            var result = await _coinGeckoClient.GetPriceAsOfFromId(coin, asOf, currency);
            result.Should().Be(coinPrice / currencyPrice);
        }

        [Fact]
        public async Task GetMarketDataAsOfFromId_should_return_valid_data_when_passing_valid_id()
        {
            var asOf = _mockCreator.GetRandomUtcDateTime();

            var coin = _mockCreator.GetRandomString(10);
            var coinPrice = _mockCreator.GetRandomPrice();
            var coinVolume = _mockCreator.GetRandomValue();
            ConfigureHistoryAsync(coin, asOf, coinPrice, coinVolume);

            var currency = _mockCreator.GetRandomString(10);
            var currencyPrice = _mockCreator.GetRandomPrice();
            var currencyVolume = _mockCreator.GetRandomValue();
            ConfigureHistoryAsync(currency, asOf, currencyPrice, currencyVolume);

            var result = await _coinGeckoClient.GetMarketDataAsOfFromId(coin, asOf, currency);
            result.AsOf.Should().Be(asOf);
            result.CoinId.Should().Be(coin);
            result.CoinSymbol.Should().Be(coin);
            result.MarketCap.Should().NotBeNull();
            result.Price.Should().Be(coinPrice / currencyPrice);
            result.Volume.Should().Be(coinVolume / currencyPrice);
            result.QuoteCurrency.Should().Be(coin);
        }

        [Fact]
        public async Task GetCoinGeckoIdFromSymbol_should_return_valid_data_when_passing_valid_id()
        {
            var id = _mockCreator.GetRandomString(10);
            var symbol = _mockCreator.GetRandomString(30);
            ConfigureListAllAsync(id, symbol, 1);
            var result = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(symbol);
            result.Should().Be(id);
        }

        [Fact]
        public async Task GetCoinGeckoIdFromSymbol_should_return_null_if_there_are_2_coins_with_the_same_symbol()
        {
            var id = _mockCreator.GetRandomString(10);
            var symbol = _mockCreator.GetRandomString(30);
            ConfigureListAllAsync(id, symbol, 2);
            var result = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(symbol);
            result.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task GetCoinList_should_return_the_full_list_when_passing_no_arguments()
        {
            ConfigureListAllAsync(count: 5);
            var result = await _coinGeckoClient.GetCoinList();
            result.Count.Should().Be(5);
        }

        [Fact]
        public async Task GetAllPrices_should_return_a_valid_list_of_prices_when_passing_valid_ids_and_currencies()
        {
            var id = _mockCreator.GetRandomString(10);
            var currency = _mockCreator.GetRandomString(10);
            var coinPrice = _mockCreator.GetRandomPrice();
            var currentPrice = _mockCreator.GetRandomPrice();
            ConfigurePriceAsync(id, currency, coinPrice, currentPrice);
            var result = await _coinGeckoClient.GetAllPrices(new[] { id }, new[] { currency });
            result.Keys.Should().Contain(id);
            result.Keys.Should().Contain(currency);
            foreach (var prices in result.Values)
            {
                prices.Keys.Should().OnlyContain(f => f == "usd");
                prices.Values.Should().OnlyContain(f => f > 0);
            }
        }

        #region Helper Methods

        private void ConfigurePriceAsync(string id, string currency, decimal? coinPrice,
            decimal? currencyPrice)
        {
            IDictionary<string, IDictionary<string, decimal?>> obj = new Dictionary<string, IDictionary<string, decimal?>>();
            obj[id] = new Dictionary<string, decimal?>
            {
                ["usd"] = coinPrice
            };
            obj[currency] = new Dictionary<string, decimal?>
            {
                ["usd"] = currencyPrice
            };
            _simpleClient.PriceAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new Response<IDictionary<string, IDictionary<string, decimal?>>>(200, null, obj));
        }

        private void ConfigureHistoryAsync(string id, DateTime date, decimal price, decimal volume)
        {
            var result = new CoinFullData
            {
                Id = id,
                Symbol = id,
                Market_data = new Market_data
                {
                    Current_price = new Dictionary<string, decimal?>
                    {
                        {"usd", price}
                    },
                    Market_cap = new Dictionary<string, decimal?>
                    {
                        {"usd", 0}
                    },
                    Total_volume = new Dictionary<string, decimal?>
                    {
                        {"usd", volume}
                    },
                }
            };
            _coinsClient.HistoryAsync(id, date.ToString("dd-MM-yyyy"), "False")
                .Returns(new Response<CoinData>(200, null, result));
        }

        private void ConfigureListAllAsync(string id = default, string symbol = default, int count = 1)
        {
            var list = Enumerable.Range(0, count)
                .Select(f => new CoinList
                {
                    Id = id ?? _mockCreator.GetRandomString(10),
                    Symbol = symbol ?? _mockCreator.GetRandomString(30)
                }).ToList();

            _coinsClient.ListAllAsync()
                .Returns(new Response<List<CoinList>>(200, null, list));
        }

        #endregion

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Trakx.Utils.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit
{
    public class CoinGeckoClientTests
    {

        private readonly ICoinGeckoClient _coinGeckoClient;
        private readonly ISimpleClient _simpleClient;
        private readonly ICoinsClient _coinsClient;
        private readonly MockCreator _mockCreator;
        private readonly IMemoryCache _memoryCache;

        public CoinGeckoClientTests(ITestOutputHelper output)
        {
            _simpleClient = Substitute.For<ISimpleClient>();
            _coinsClient = Substitute.For<ICoinsClient>();
            _memoryCache = Substitute.For<IMemoryCache>();
            _mockCreator = new MockCreator(output);

            _coinGeckoClient = new CoinGeckoClient(_memoryCache, _coinsClient, _simpleClient);
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
        public async Task GetMarketDataAsOfFromId_should_return_valid_data_when_passing_valid_id()
        {
            var asOf = _mockCreator.GetRandomUtcDateTime();
            var asOfString = asOf.ToString("dd-MM-yyyy");

            var coin = _mockCreator.GetRandomString(10);
            var coinPrice = _mockCreator.GetRandomPrice();
            var coinVolume = _mockCreator.GetRandomValue();
            ConfigureHistoryAsync(coin, asOf, coinPrice, coinVolume);

            var currency = _mockCreator.GetRandomString(10);
            var currencyPrice = _mockCreator.GetRandomPrice();
            var currencyVolume = _mockCreator.GetRandomValue();
            ConfigureHistoryAsync(currency, asOf, currencyPrice, currencyVolume);

            var result = await _coinGeckoClient.GetMarketDataAsOfFromId(coin, asOf, currency);
            result!.AsOf.Should().Be(asOf);
            result.CoinId.Should().Be(coin);
            result.CoinSymbol.Should().Be(coin);
            result.MarketCap.Should().NotBeNull();
            result.Price.Should().Be(coinPrice / currencyPrice);
            result.Volume.Should().Be(coinVolume / currencyPrice);
            result.QuoteCurrency.Should().Be(coin);

            Expression<Predicate<object>> fxRatePredicate =
                o =>  o.ToString()!.Contains(asOfString) && o.ToString()!.Contains("fx-rate") && o.ToString()!.Contains(currency);
            Expression<Predicate<object>> marketDataPredicate =
                o =>  o.ToString()!.Contains(asOfString) && o.ToString()!.Contains("market-data") && o.ToString()!.Contains(currency) && o.ToString()!.Contains(coin);

            _memoryCache.Received(1).TryGetValue(Arg.Is(fxRatePredicate), out _);
            _memoryCache.Received(1).CreateEntry(Arg.Is(fxRatePredicate));
            _memoryCache.Received(1).TryGetValue(Arg.Is(marketDataPredicate), out _);
            _memoryCache.Received(1).CreateEntry(Arg.Is(marketDataPredicate));
        }

        [Fact]
        public async Task GetCoinGeckoIdFromSymbol_should_return_valid_data_when_passing_valid_id()
        {
            var id = _mockCreator.GetRandomString(10);
            var symbol = _mockCreator.GetRandomString(30);
            ConfigureListAllAsync(id, symbol);
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
            _memoryCache.Received(1).CreateEntry(Arg.Is<object>(o => o.ToString()!.Contains("coin-list")));
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

        [Fact]
        public async Task GetMarketDataForDateRange_should_call_Range_and_transform_data()
        {
            var dates = new double[] {1619756926435, 1619757185872};
            var range = new Range
            {
                Market_caps = new List<TimestampedValue>
                {
                    new() { dates[0], 318992245176.35913 },
                    new() { dates[1], 319632242563.95764 },
                },
                Total_volumes = new List<TimestampedValue>
                {
                    new() { dates[0], 38069451649.54143 },
                    new() { dates[1], 38825217290.29339 },
                },
                Prices = new List<TimestampedValue>
                {
                    new() { dates[0], 2756.166102270321 },
                    new() { dates[1], 2761.460672838776 },
                }
            };
            var id = _mockCreator.GetRandomString(5);
            var vsCurrency = _mockCreator.GetRandomString(3);
            var start = _mockCreator.GetRandomUtcDateTimeOffset();
            var end = _mockCreator.GetRandomUtcDateTimeOffset();

            _coinsClient.RangeAsync(id, vsCurrency, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), CancellationToken.None)
                .Returns(new Response<Range>(200, null, range));

            var result = await _coinGeckoClient.GetMarketDataForDateRange(id, vsCurrency, start, end, CancellationToken.None)
                .ConfigureAwait(false);

            await _coinsClient.Received(1)
                .RangeAsync(id, vsCurrency, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), CancellationToken.None)
                .ConfigureAwait(false);

            result.Keys.Should().BeEquivalentTo(dates.Select(d => DateTimeOffset.FromUnixTimeMilliseconds((long)d)));

            var firstDate = DateTimeOffset.FromUnixTimeMilliseconds((long)dates[0]);
            var firstResult = result[firstDate];
            firstResult.Price.Should().Be((decimal)2756.166102270321);
            firstResult.Volume.Should().Be((decimal)38069451649.54143);
            firstResult.MarketCap.Should().Be((decimal)318992245176.35913);
            firstResult.CoinId.Should().Be(id);
            firstResult.QuoteCurrency.Should().Be(vsCurrency);
            firstResult.AsOf.Should().Be(firstDate);
            firstResult.CoinSymbol.Should().BeNull();
        }

        #region Helper Methods

        private void ConfigurePriceAsync(string id, string currency, decimal? coinPrice,
            decimal? currencyPrice)
        {
            IDictionary<string, IDictionary<string, decimal?>> obj = new Dictionary<string, IDictionary<string, decimal?>>();
            obj[id] = new Dictionary<string, decimal?>
            {
                [Constants.Usd] = coinPrice
            };
            obj[currency] = new Dictionary<string, decimal?>
            {
                [Constants.Usd] = currencyPrice
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
                        {Constants.Usd, price}
                    },
                    Market_cap = new Dictionary<string, decimal?>
                    {
                        {Constants.Usd, 0}
                    },
                    Total_volume = new Dictionary<string, decimal?>
                    {
                        {Constants.Usd, volume}
                    },
                }
            };
            _coinsClient.HistoryAsync(id, date.ToString("dd-MM-yyyy"), "False")
                .Returns(new Response<CoinData>(200, null, result));
        }

        private void ConfigureListAllAsync(string? id = default, string? symbol = default, int count = 1)
        {
            var list = Enumerable.Range(0, count)
                .Select(_ => new CoinList
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

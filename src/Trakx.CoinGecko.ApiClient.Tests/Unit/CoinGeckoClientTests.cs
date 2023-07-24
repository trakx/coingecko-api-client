using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Trakx.Common.ApiClient.Extensions;
using Trakx.Common.Testing.Mocks;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public class CoinGeckoClientTests
{
    private readonly ISimpleClient _simpleClient;
    private readonly ICoinsClient _coinsClient;
    private readonly MockCreator _mockCreator;
    private readonly IMemoryCache _memoryCache;
    private readonly CoinGeckoClient _coinGeckoClient;

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
        var id = _mockCreator.GetString(10);
        var currency = _mockCreator.GetString(5);
        var coinPrice = _mockCreator.GetPrice();
        var currencyPrice = _mockCreator.GetPrice();

        ConfigurePriceAsync(id, currency, coinPrice, currencyPrice);

        ConfigureSupportedQuoteCurrencies(Constants.Usd);

        var result = await _coinGeckoClient.GetLatestPrice(id, currency);

        result.Should().Be(coinPrice / currencyPrice);
    }

    [Fact]
    public async Task GetMarketDataAsOfFromId_should_return_valid_data_when_passing_valid_id()
    {
        var asOf = _mockCreator.GetUtcDateTime();
        var asOfString = CoinGeckoClient.GetDateString(asOf);

        var coin = _mockCreator.GetString(10);
        var coinPrice = _mockCreator.GetPrice();
        var coinVolume = _mockCreator.GetValue();

        ConfigureHistoryAsync(coin, asOf, coinPrice, coinVolume);

        var currency = _mockCreator.GetString(10);
        var currencyPrice = _mockCreator.GetPrice();
        var currencyVolume = _mockCreator.GetValue();
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
            o => o.ToString()!.Contains(asOfString) && o.ToString()!.Contains("fx-rate") && o.ToString()!.Contains(currency);
        Expression<Predicate<object>> marketDataPredicate =
            o => o.ToString()!.Contains(asOfString) && o.ToString()!.Contains("market-data") && o.ToString()!.Contains(currency) && o.ToString()!.Contains(coin);

        _memoryCache.Received(1).TryGetValue(Arg.Is(fxRatePredicate), out _);
        _memoryCache.Received(1).CreateEntry(Arg.Is(fxRatePredicate));
        _memoryCache.Received(1).TryGetValue(Arg.Is(marketDataPredicate), out _);
        _memoryCache.Received(1).CreateEntry(Arg.Is(marketDataPredicate));
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_should_return_valid_data_when_passing_valid_id()
    {
        var id = _mockCreator.GetString(10);
        var symbol = _mockCreator.GetString(30);
        ConfigureListAllAsync(id, symbol);
        var result = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(symbol);
        result.Should().Be(id);
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_should_return_null_if_there_are_2_coins_with_the_same_symbol()
    {
        var id = _mockCreator.GetString(10);
        var symbol = _mockCreator.GetString(30);
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
    public async Task GetAllPrices_should_return_multiple_prices_when_passing_valid_ids_and_currencies()
    {
        var id = _mockCreator.GetString(10);
        var currency = _mockCreator.GetString(10);
        var coinPrice = _mockCreator.GetPrice();
        var currentPrice = _mockCreator.GetPrice();

        ConfigurePriceAsync(id, currency, coinPrice, currentPrice);

        var baseIds = id.AsSingletonArray();
        var quoteIds = currency.AsSingletonArray();
        var supportedQuoteCurrencies = ConfigureSupportedQuoteCurrencies(Constants.Usd);

        var result = await _coinGeckoClient.GetAllPrices(baseIds, quoteIds);

        Integration.CoinGeckoClientTests.AssertMultiplePrices(result, baseIds, quoteIds, supportedQuoteCurrencies);
    }

    [Fact]
    public async Task GetMarketDataForDateRange_should_call_Range_and_transform_data()
    {
        var dates = new double[] { 1619756926435, 1619757185872 };
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
        var id = _mockCreator.GetString(5);
        var vsCurrency = _mockCreator.GetString(3);
        var start = _mockCreator.GetUtcDateTimeOffset();
        var end = _mockCreator.GetUtcDateTimeOffset();

        _coinsClient
            .RangeAsync(id, vsCurrency, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), CancellationToken.None)
            .Returns(range.AsResponse());

        var result = await _coinGeckoClient
            .GetMarketDataForDateRange(id, vsCurrency, start, end, CancellationToken.None)
            .ConfigureAwait(false);

        await _coinsClient
            .Received(1)
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

    private void ConfigurePriceAsync(string id, string currency, decimal coinPrice, decimal currencyPrice)
    {
        var bag = MultiplePricesTests.MakePriceBag();
        bag[id] = BagDecimal(coinPrice);
        bag[currency] = BagDecimal(currencyPrice);

        _simpleClient
            .PriceAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(bag.AsResponse());
    }

    private void ConfigureHistoryAsync(string id, DateTime date, decimal price, decimal volume)
    {
        var result = new CoinFullData
        {
            Id = id,
            Symbol = id,
            Market_data = new Market_data
            {
                Market_cap = BagDecimal(0),
                Total_volume = BagDecimal(volume),
                Current_price = BagDecimal(price),
            }
        };

        var timestamp = CoinGeckoClient.GetDateString(date);

        _coinsClient
            .HistoryAsync(id, timestamp, localization: false)
            .Returns(((CoinData)result).AsResponse());
    }

    private void ConfigureListAllAsync(string? id = default, string? symbol = default, int count = 1)
    {
        var list = Enumerable.Range(0, count)
            .Select(_ => new CoinList
            {
                Id = id ?? _mockCreator.GetString(10),
                Symbol = symbol ?? _mockCreator.GetString(30)
            }).ToList();

        _coinsClient
            .ListAllAsync()
            .Returns(list.AsResponse());
    }

    private List<string> ConfigureSupportedQuoteCurrencies(params string[] quoteCurrencies)
    {
        var supportedQuoteCurrencies = quoteCurrencies.ToList();

        _simpleClient
            .Supported_vs_currenciesAsync()
            .Returns(supportedQuoteCurrencies.AsResponse());

        return supportedQuoteCurrencies;
    }

    internal static Dictionary<string, decimal?> BagDecimal(decimal value, string currency = Constants.Usd)
    {
        return new() { [currency] = value };
    }
    #endregion

}

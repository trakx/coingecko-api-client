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
    internal const string VsCurrency = Constants.Usd;
    private const string First = nameof(First);
    private const string Second = nameof(Second);

    private readonly ISimpleClient _simpleClient;
    private readonly ICoinsClient _coinsClient;
    private readonly MockCreator _mockCreator;
    private readonly IMemoryCache _memoryCache;

    private readonly CoinGeckoClient _coinGeckoClient;

    private readonly string _coin;
    private readonly DateTimeOffset _start;
    private readonly DateTimeOffset _end;

    public CoinGeckoClientTests(ITestOutputHelper output)
    {
        _simpleClient = Substitute.For<ISimpleClient>();
        _coinsClient = Substitute.For<ICoinsClient>();
        _memoryCache = Substitute.For<IMemoryCache>();
        _mockCreator = new MockCreator(output);

        var now = _mockCreator.GetUtcDateTimeOffset();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNowAsOffset.Returns(now);

        _coinGeckoClient = new CoinGeckoClient(_memoryCache, _coinsClient, _simpleClient, dateTimeProvider);

        _coin = _mockCreator.GetString(5);
        _start = now.AddMonths(-2);
        _end = now.AddMonths(-1);
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_caches_results()
    {
        ConfigureListAllAsync();
        _ = await _coinGeckoClient.GetCoinGeckoIdFromSymbol("symbol");
        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCoinList_caches_results()
    {
        ConfigureListAllAsync();
        _ = await _coinGeckoClient.GetCoinList();
        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSupportedQuoteCurrencies_caches_results()
    {
        ConfigureSupportedQuoteCurrencies(Constants.Usd);
        _ = await _coinGeckoClient.GetSupportedQuoteCurrencies();
        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMarketDataAsOfFromId_caches_results()
    {
        var date = _start.DateTime;

        ConfigureHistoryAsync(_coin, date);
        ConfigureHistoryAsync(VsCurrency, date);

        _ = await _coinGeckoClient.GetMarketDataAsOfFromId(_coin, date, VsCurrency);
        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMarketData_uses_cached_results_if_over_1_day()
    {
        SetupMarketDataResponse();

        _ = await _coinGeckoClient.GetMarketData(_coin, VsCurrency, 22);

        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
        _coinsClient.ReceivedCalls().Should().BeEmpty();
        _simpleClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarketDataForDateRange_uses_cached_results_if_range_ended_at_least_2_days_ago()
    {
        SetupMarketDataResponse();

        _ = await _coinGeckoClient.GetMarketDataForDateRange(_coin, VsCurrency, _start, _end);

        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
        _coinsClient.ReceivedCalls().Should().BeEmpty();
        _simpleClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Search_caches_results()
    {
        _ = await _coinGeckoClient.Search();
        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMarketRank_caches_results()
    {
        _ = await _coinGeckoClient.GetMarketRank();
        _memoryCache.ReceivedCalls().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLatestPrice_should_return_valid_price_when_passing_valid_id()
    {
        var currency = _mockCreator.GetString(5);
        var coinPrice = _mockCreator.GetPrice();
        var currencyPrice = _mockCreator.GetPrice();

        ConfigurePriceAsync(_coin, currency, coinPrice, currencyPrice);

        ConfigureSupportedQuoteCurrencies(Constants.Usd);

        var result = await _coinGeckoClient.GetLatestPrice(_coin, currency);

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
        var symbol = _mockCreator.GetString(30);
        ConfigureListAllAsync(_coin, symbol);
        var result = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(symbol);
        result.Should().Be(_coin);
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_should_return_highest_ranked_coin_if_there_are_multiple_coins_with_the_same_symbol()
    {
        var symbol = _mockCreator.GetString(30);
        ConfigureListAllAsync(symbol: symbol, count: 2);

        var marketData = new List<SearchCoinData>
        {
            new() { Id = "null", Symbol = symbol, Market_cap_rank = null },
            new() { Id = First, Symbol = symbol, Market_cap_rank = 1 },
            new() { Id = Second, Symbol = Second, Market_cap_rank  = 2 },
            new() { Id = "third", Symbol = symbol, Market_cap_rank = 3 },
        };

        SetupMarketsPage(marketData);

        var result = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(symbol);
        result.Should().Be(First);
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
        var currency = _mockCreator.GetString(10);
        var coinPrice = _mockCreator.GetPrice();
        var currentPrice = _mockCreator.GetPrice();

        ConfigurePriceAsync(_coin, currency, coinPrice, currentPrice);

        var baseIds = _coin.AsSingletonArray();
        var quoteIds = currency.AsSingletonArray();
        var supportedQuoteCurrencies = ConfigureSupportedQuoteCurrencies(Constants.Usd);

        var result = await _coinGeckoClient.GetAllPrices(baseIds, quoteIds);

        Integration.CoinGeckoClientTests.AssertMultiplePrices(result, baseIds, quoteIds, supportedQuoteCurrencies);
    }

    [Fact]
    public async Task GetMarketDataForDateRange_should_call_Range_and_transform_data()
    {
        var dates = new double[] { 1619756926435, 1619757185872 };

        var range = CreateRange(dates);
        SetupRangeResponse(_coin, VsCurrency, _start, _end, range);

        var result = await _coinGeckoClient
            .GetMarketDataForDateRange(_coin, VsCurrency, _start, _end);

        await _coinsClient
            .Received(1)
            .RangeAsync(_coin, VsCurrency, _start.ToUnixTimeSeconds(), _end.ToUnixTimeSeconds());

        result.Keys.Should().BeEquivalentTo(dates.Select(d => DateTimeOffset.FromUnixTimeMilliseconds((long)d)));

        var firstDate = DateTimeOffset.FromUnixTimeMilliseconds((long)dates[0]);
        var firstResult = result[firstDate];
        firstResult.Price.Should().Be((decimal)range.Prices[0][1]);
        firstResult.Volume.Should().Be((decimal)range.Total_volumes[0][1]);
        firstResult.MarketCap.Should().Be((decimal)range.Market_caps[0][1]);
        firstResult.CoinId.Should().Be(_coin);
        firstResult.QuoteCurrency.Should().Be(VsCurrency);
        firstResult.AsOf.Should().Be(firstDate);
        firstResult.CoinSymbol.Should().BeNull();
    }

    [Fact]
    public async Task GetMarketRank_should_reuse_previous_results_when_called_with_limit_under_default()
    {
        var marketData = new List<SearchCoinData>
        {
            new() { Id = First, Symbol = First, Market_cap_rank = 1 },
            new() { Id = Second, Symbol = Second, Market_cap_rank  = 2 },
            new() { Id = "third", Symbol = "third", Market_cap_rank = 3 },
        };

        SetupMarketsPage(marketData);

        var result = await _coinGeckoClient.GetMarketRank(); // get the default amount

        var initialApiCalls = _coinsClient.GetReceivedCalls(nameof(_coinsClient.MarketsAsync)).Count();

        var cacheKey = $"{typeof(CoinGeckoClient).FullName}|market-rank|{ICoinGeckoClient.MarketRankDefaultLimit}";
        _memoryCache
            .TryGetValue(cacheKey, out Arg.Any<object?>())
            .Returns(call =>
            {
                call[1] = result.ToList();
                return true;
            });

        _ = await _coinGeckoClient.GetMarketRank(limit: 1);

        var updatedApiCalls = _coinsClient.GetReceivedCalls(nameof(_coinsClient.MarketsAsync)).Count();

        updatedApiCalls.Should().Be(initialApiCalls);
    }

    private void ConfigurePriceAsync(string _coin, string currency, decimal coinPrice, decimal currencyPrice)
    {
        var bag = MultiplePricesTests.MakePriceBag();
        bag[_coin] = BagDecimal(coinPrice);
        bag[currency] = BagDecimal(currencyPrice);

        _simpleClient
            .PriceAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(bag.AsResponse());
    }

    private void ConfigureHistoryAsync(string? _coin = null, DateTime? date = null, decimal? price = null, decimal? volume = null)
    {
        var idValue = _coin ?? _mockCreator.GetString(10);

        var result = new CoinFullData
        {
            Id = idValue,
            Symbol = idValue,
            Market_data = new Market_data
            {
                Market_cap = BagDecimal(0),
                Total_volume = BagDecimal(volume ?? _mockCreator.GetPrice()),
                Current_price = BagDecimal(price ?? _mockCreator.GetPrice()),
            }
        };

        var timestamp = CoinGeckoClient.GetDateString(date ?? _mockCreator.GetUtcDateTime());

        _coinsClient
            .HistoryAsync(idValue, timestamp, localization: false)
            .Returns(((CoinData)result).AsResponse());
    }

    private void ConfigureListAllAsync(string? _coin = default, string? symbol = default, int count = 1)
    {
        var list = Enumerable.Range(0, count)
            .Select(_ => new CoinList
            {
                Id = _coin ?? _mockCreator.GetString(10),
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

    private void SetupMarketsPage(List<SearchCoinData> marketData, int page = 1)
    {
        _coinsClient.MarketsAsync(
            vs_currency: ICoinGeckoClient.MainQuoteCurrency,
            ids: Arg.Any<string?>(),
            category: Arg.Any<string>(),
            order: Arg.Any<string>(),
            per_page: Arg.Any<int?>(),
            page: page,
            cancellationToken: Arg.Any<CancellationToken>())

        .Returns(marketData.AsResponse());
    }

    private void SetupRangeResponse(string coin, string vsCurrency, DateTimeOffset start, DateTimeOffset end, Range range)
    {
        _coinsClient
            .RangeAsync(coin, vsCurrency, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds())
            .Returns(range.AsResponse());
    }

    private static Range CreateRange(params double[] dates)
    {
        return new Range
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
    }

    private void SetupMarketDataResponse()
    {
        var marketData = new Dictionary<DateTimeOffset, MarketData>();

        _memoryCache
            .TryGetValue(Arg.Any<string>(), out Arg.Any<object?>())
            .Returns(call =>
            {
                call[1] = marketData;
                return true;
            });
    }
}

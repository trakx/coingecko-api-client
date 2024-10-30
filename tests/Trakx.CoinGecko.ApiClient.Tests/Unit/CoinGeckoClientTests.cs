using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using Trakx.Common.Testing.Mocks;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public partial class CoinGeckoClientTests
{
    internal const string VsCurrency = Constants.Usd;

    protected const string Symbol = nameof(Symbol);
    protected const string First = nameof(First);
    protected const string Second = nameof(Second);
    protected const string Third = nameof(Third);

    protected readonly ISimpleClient _simpleClient;
    protected readonly ISearchClient _searchClient;
    protected readonly ICoinsClient _coinsClient;
    protected readonly MockCreator _mockCreator;
    protected readonly IMemoryCache _memoryCache;

    protected readonly CoinGeckoClient _coinGeckoClient;

    protected readonly string _coin;
    protected readonly DateTimeOffset _start;
    protected readonly DateTimeOffset _end;

    public CoinGeckoClientTests(ITestOutputHelper output)
    {
        _simpleClient = Substitute.For<ISimpleClient>();
        _searchClient = Substitute.For<ISearchClient>();
        _coinsClient = Substitute.For<ICoinsClient>();
        _memoryCache = Substitute.For<IMemoryCache>();
        _mockCreator = new MockCreator(output);

        var now = _mockCreator.GetUtcDateTimeOffset();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNowAsOffset.Returns(now);

        _coinGeckoClient = new CoinGeckoClient(
            _memoryCache,
            _coinsClient,
            _simpleClient,
            _searchClient,
            dateTimeProvider);

        _coin = _mockCreator.GetString(5);
        _start = now.AddMonths(-2);
        _end = now.AddMonths(-1);
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
            new() { Id = Third, Symbol = "third", Market_cap_rank = 3 },
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

    [Fact]
    public async Task GetAllPricesForSymbols_throws_if_symbols_are_null()
    {
        var action = async () => _ = await _coinGeckoClient.GetAllPricesForSymbols(null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetAllPricesForSymbols_gets_prices_for_all_mapped_coingecko_ids()
    {
        // TODO
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetAllPricesForSymbols_returns_expected_data()
    {
        // TODO
        await Task.CompletedTask;
    }

}

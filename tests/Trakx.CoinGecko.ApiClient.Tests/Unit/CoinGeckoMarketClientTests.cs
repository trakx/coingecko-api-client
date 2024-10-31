using System.Linq.Expressions;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

// will soon become CoinGeckoMarketClientTests
public partial class CoinGeckoClientTests
{
    [Fact]
    public async Task GetMarketData_uses_cached_results_if_over_1_day()
    {
        const int days = 22;

        SetupMarketDataResponse();

        _ = await _coinGeckoClient.GetMarketData(_coin, VsCurrency, days);

        AssertReusedCachedEntry("chart", _coin, VsCurrency, days);

        _coinsClient.ReceivedCalls().Should().BeEmpty();
        _simpleClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarketDataForDateRange_uses_cached_results_if_range_ended_at_least_2_days_ago()
    {
        SetupMarketDataResponse();

        _ = await _coinGeckoClient.GetMarketDataForDateRange(_coin, VsCurrency, _start, _end);

        AssertReusedCachedEntry("range", _coin, VsCurrency, _start.ToUnixTimeSeconds(), _end.ToUnixTimeSeconds());

        _coinsClient.ReceivedCalls().Should().BeEmpty();
        _simpleClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarketDataForDateRange_should_call_Range_and_transform_data()
    {
        var dates = new double[] { 161_975_692_6435, 161_975_718_5872 };

        var range = CreateRange(dates);
        SetupRangeResponse(_coin, VsCurrency, _start, _end, range);

        var result = await _coinGeckoClient
            .GetMarketDataForDateRange(_coin, VsCurrency, _start, _end);

        await _coinsClient
            .Received(1)
            .RangeAsync(_coin, VsCurrency, _start.ToUnixTimeSeconds(), _end.ToUnixTimeSeconds());

        result.Keys.Should().BeEquivalentTo(dates.Select(d => d.AsUnixMillisToDate()));

        var firstDate = dates[0].AsUnixMillisToDate();
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
    public async Task GetMarketDataAsOfFromId_caches_results()
    {
        var date = _start.DateTime;

        ConfigureHistoryAsync(_coin, date);
        ConfigureHistoryAsync(VsCurrency, date);

        _ = await _coinGeckoClient.GetMarketDataAsOfFromId(_coin, date, VsCurrency);

        AssertCachedEntry("market-data", _coin, VsCurrency, date.ToDateString());
    }

    [Fact]
    public async Task GetMarketDataAsOfFromId_should_return_valid_data_when_passing_valid_id()
    {
        var asOf = _mockCreator.GetUtcDateTime();
        var asOfString = asOf.ToDateString();

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

        Expression<Predicate<object>> fxRatePredicate = o => TextContainsAll(o, asOfString, "fx-rate", currency);
        _memoryCache.Received(1).TryGetValue(Arg.Is(fxRatePredicate), out _);
        _memoryCache.Received(1).CreateEntry(Arg.Is(fxRatePredicate));

        Expression<Predicate<object>> marketDataPredicate = o => TextContainsAll(o, asOfString, "market-data", currency, coin);
        _memoryCache.Received(1).TryGetValue(Arg.Is(marketDataPredicate), out _);
        _memoryCache.Received(1).CreateEntry(Arg.Is(marketDataPredicate));
    }

    [Fact]
    public async Task GetMarketRank_caches_results()
    {
        _ = await _coinGeckoClient.GetMarketRank();
        AssertCachedEntry("market-rank", ICoinGeckoMarketClient.MarketRankDefaultLimit);
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
    public async Task Search_caches_results()
    {
        _ = await _coinGeckoClient.Search();
        AssertCachedEntry("search", ICoinGeckoMarketClient.MainQuoteCurrency, ICoinGeckoMarketClient.DefaultSearchOrder);
    }

    private bool TextContainsAll(object o, params string[] fragments)
    {
        if (o == null) return false;
        return fragments.All(o.ToString()!.Contains);
    }
}

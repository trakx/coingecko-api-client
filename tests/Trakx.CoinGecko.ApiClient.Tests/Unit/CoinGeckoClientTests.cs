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
    public async Task GetAllPrices_should_return_multiple_prices_when_passing_valid_ids_and_currencies()
    {
        var currency = _mockCreator.GetString(10);
        var coinPrice = _mockCreator.GetPrice();
        var currentPrice = _mockCreator.GetPrice();

        ConfigurePriceAsync(_coin, currency, coinPrice, currentPrice);

        string[] baseIds = [_coin];
        string[] quoteIds = [currency];
        var supportedQuoteCurrencies = ConfigureSupportedQuoteCurrencies(Constants.Usd);

        var result = await _coinGeckoClient.GetAllPrices(baseIds, quoteIds);

        Integration.CoinGeckoClientTests.AssertMultiplePrices(result, baseIds, quoteIds, supportedQuoteCurrencies);
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

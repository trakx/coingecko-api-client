namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

// will soon become CoinGeckoPricesClientTests
public partial class CoinGeckoClientTests
{
    [Fact]
    public async Task GetLatestPrice_returns_valid_price_when_passing_valid_id()
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
    public async Task GetAllPrices_returns_multiple_prices_when_passing_valid_ids_and_currencies()
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
}

using System.Collections.Generic;
using Trakx.CoinGecko.ApiClient.Models;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public class MultiplePricesTests
{
    private const string Coin1 = "coin1";

    [Fact]
    public void GetPrice_returns_direct_price()
    {
        const string mainCurrency = "eur";

        var source = MakePriceBag();
        source[Coin1] = MakeDecimalBag();
        source["coin2"] = MakeDecimalBag();
        source[Coin1][mainCurrency] = 100m;
        source["coin2"][mainCurrency] = 200m;

        var prices = new MultiplePrices(source);

        var price = prices.GetPrice(Coin1, mainCurrency);
        price.Should().Be(100m);
    }

    [Fact]
    public void GetPrice_returns_converted_price()
    {
        const string mainCurrency = "eur";
        const string wantedQuoteCurrency = "gbp";

        const decimal basePriceInMainCurrency = 100m;

        const decimal rateBetweenWantedAndMain = 1.17m; // the price of 1 GBP in EUR
        const decimal rateBetweenMainAndWanted = 1 / rateBetweenWantedAndMain;

        // in pure maths, the formula is: B / Q = (B / M) * (M / Q)
        // base / wanted quote = (base / main currency) * (main currency / wanted quote)
        const decimal expectedPrice = basePriceInMainCurrency * rateBetweenMainAndWanted;

        var source = MakePriceBag();
        source[Coin1] = MakeDecimalBag();
        source["coin2"] = MakeDecimalBag();
        source[wantedQuoteCurrency] = MakeDecimalBag();

        source[Coin1][mainCurrency] = basePriceInMainCurrency;
        source[wantedQuoteCurrency][mainCurrency] = rateBetweenWantedAndMain;

        var prices = new MultiplePrices(source);

        var price = prices.GetPrice(Coin1, wantedQuoteCurrency);
        price.Should().Be(expectedPrice);
    }

    internal static IDictionary<string, IDictionary<string, decimal?>> MakePriceBag()
    {
        return new Dictionary<string, IDictionary<string, decimal?>>();
    }

    internal static IDictionary<string, decimal?> MakeDecimalBag()
    {
        return new Dictionary<string, decimal?>();
    }
}

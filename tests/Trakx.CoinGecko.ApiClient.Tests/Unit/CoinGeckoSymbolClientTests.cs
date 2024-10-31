using Trakx.Common.ApiClient.Extensions;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

// will soon become CoinGeckoSymbolClientTests
public partial class CoinGeckoClientTests
{
    [Fact]
    public async Task GetCoinList_caches_results()
    {
        ConfigureListAllAsync();
        _ = await _coinGeckoClient.GetCoinList();
        AssertCachedEntry("coin-list");
    }

    [Fact]
    public async Task GetCoinList_returns_expected_coins()
    {
        ConfigureListAllAsync(count: 5);
        var result = await _coinGeckoClient.GetCoinList();
        result.Count.Should().Be(5);
    }

    [Fact]
    public async Task GetSupportedQuoteCurrencies_caches_results()
    {
        ConfigureSupportedQuoteCurrencies(Constants.Usd);
        _ = await _coinGeckoClient.GetSupportedQuoteCurrencies();
        AssertCachedEntry("supported-vs-currencies");
    }

    [Fact]
    public async Task GetSupportedQuoteCurrencies_returns_expected_currencies()
    {
        ConfigureSupportedQuoteCurrencies(Constants.Usd, Constants.UsdCoin, "eur");
        var result = await _coinGeckoClient.GetSupportedQuoteCurrencies();
        result.Should().BeEquivalentTo([Constants.Usd, Constants.UsdCoin, "eur"]);
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_caches_results()
    {
        ConfigureListAllAsync();
        _ = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(Symbol);
        AssertCachedEntry("id-from-symbol", Symbol);
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_queries_markets_for_highest_ranked_symbols()
    {
        // no market rank setup

        var result = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(Symbol);
        result.Should().Be(null);

        await _coinsClient
            .ReceivedWithAnyArgs()
            .MarketsAsync(Arg.Any<string>(), result, null, null, null, null);
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_returns_highest_ranked_coin_if_there_are_multiple_coins_with_the_same_symbol()
    {
        ConfigureListAllAsync(symbol: Symbol, count: 2);

        var marketData = new List<SearchCoinData>
        {
            new() { Id = "null", Symbol = Symbol, Market_cap_rank = null },
            new() { Id = First, Symbol = Symbol, Market_cap_rank = 1 },
            new() { Id = Second, Symbol = Second, Market_cap_rank  = 2 },
            new() { Id = Third, Symbol = Symbol, Market_cap_rank = 3 },
        };

        SetupMarketsPage(marketData);

        var result = await _coinGeckoClient.GetCoinGeckoIdFromSymbol(Symbol);
        result.Should().Be(First);
    }

    [Fact]
    public async Task GetCoinsFromSymbol_caches_result()
    {
        _ = await _coinGeckoClient.GetCoinsFromSymbol(Symbol);
        AssertCachedEntry($"coins-from-symbol", Symbol);
    }

    [Fact]
    public async Task GetCoinsFromSymbol_returns_coins_from_api_matching_exact_symbol()
    {
        var coin1 = new Coins { Symbol = Symbol, Api_symbol = "id1" };
        var coin2 = new Coins { Symbol = Symbol + "2", Api_symbol = "id2" };
        var coin3 = new Coins { Symbol = Symbol, Api_symbol = "id3" };

        Search search = new()
        {
            Coins = [coin1, coin2, coin3]
        };

        _searchClient.SearchDataAsync(Symbol).Returns(search.AsResponse());

        var result = await _coinGeckoClient.GetCoinsFromSymbol(Symbol);

        result.Should().BeEquivalentTo([coin1, coin3]);
    }

    [Fact]
    public async Task MapRankedSymbolsToCoinGeckoIds_caches_result()
    {
        _ = await _coinGeckoClient.MapRankedSymbolsToCoinGeckoIds();
        AssertCachedEntry("symbol-to-ids-map");
    }

    [Fact]
    public async Task MapRankedSymbolsToCoinGeckoIds_includes_all_ranked_coins_in_map()
    {
        List<SearchCoinData> marketData =
        [
            new() { Id = First, Symbol = Symbol, Market_cap_rank = 1 },
            new() { Id = Second, Symbol = Second, Market_cap_rank  = 2 },
            new() { Id = Third, Symbol = Symbol, Market_cap_rank = 3 },
            new() { Id = "scam", Symbol = Symbol, Market_cap_rank = null },
        ];

        SetupMarketsPage(marketData);

        var map = await _coinGeckoClient.MapRankedSymbolsToCoinGeckoIds();

        map.Should().ContainKey(Symbol).WhoseValue.Should().BeEquivalentTo([First, Third, "scam"]);

        map.Should().ContainKey(Second).WhoseValue.Should().BeEquivalentTo(Second);
    }
}

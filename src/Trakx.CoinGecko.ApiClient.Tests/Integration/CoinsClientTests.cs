using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class CoinsClientTests : CoinGeckoClientTestBase
{
    private readonly ISimpleClient _simpleClient;
    private readonly ICoinsClient _coinsClient;
    private readonly ITestOutputHelper _output;

    public CoinsClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
        : base(apiFixture, output)
    {
        _coinsClient = ServiceProvider.GetRequiredService<ICoinsClient>();
        _simpleClient = ServiceProvider.GetRequiredService<ISimpleClient>();
        _output = output;
    }

    [Fact]
    public async Task ListAllAsync_should_all_coins_from_the_coingecko()
    {
        for (int i = 0; i < 15; i++)
        {
            var result = await _coinsClient.ListAllAsync();
            var list = result.Result;
            list.Should().NotBeEmpty();
            EnsureAllJsonElementsWereMapped(list);
        }

    }

    [Theory]
    [ClassData(typeof(CoinGeckoIdsTestData))]
    public async Task CoinsAsync_should_return_a_valid_coindata_when_passing_a_valid_id(string id)
    {
        var result = await _coinsClient.CoinsAsync(id, "false");
        var list = result.Result;
        Assert.NotNull(list);
    }

    [Theory]
    [ClassData(typeof(CoinGeckoIdsTestData))]
    public async Task HistoryAsync_should_historical_data_when_passing_valid_id(string id)
    {
        var history = await _coinsClient.HistoryAsync(id, "30-01-2021", "true");
        history.StatusCode.Should().Be((int)HttpStatusCode.OK);
        history.Result.Id.Should().Be(id);
        history.Result.Symbol.Should().NotBeNullOrWhiteSpace();
        history.Result.Image.Thumb.Should().NotBeEmpty();
        history.Result.Image.Small.Should().NotBeEmpty();
        history.Result.Market_data.Current_price.Should().NotBeEmpty();
        history.Result.Market_data.Market_cap.Should().NotBeEmpty();
        EnsureAllJsonElementsWereMapped(history);
    }

    [Fact]
    public async Task RangeAsync_should_return_historical_data_for_a_range_of_dates()
    {
        var start = DateTimeOffset.Parse("2020-12-01");
        var end = DateTimeOffset.Parse("2020-12-31");

        var range = await _coinsClient
            .RangeAsync("ethereum", "usd", start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), CancellationToken.None)
            .ConfigureAwait(false);

        range.StatusCode.Should().Be((int)HttpStatusCode.OK);
        range.Result.Prices.Count.Should().BeGreaterThan(30);
        range.Result.Market_caps.Count.Should().Be(range.Result.Prices.Count);
        range.Result.Total_volumes.Count.Should().Be(range.Result.Prices.Count);

        var first = range.Result.Prices.First();
        DateTimeOffset
            .FromUnixTimeMilliseconds((long)first[0])
            .Should().BeCloseTo(start, TimeSpan.FromDays(1));
        first[1].Should().BeApproximately(613d, 15d);

        var last = range.Result.Prices.Last();
        DateTimeOffset
            .FromUnixTimeMilliseconds((long)last[0])
            .Should().BeCloseTo(end, TimeSpan.FromDays(1));
        last[1].Should().BeApproximately(746d, 10d);

        EnsureAllJsonElementsWereMapped(range);
    }

    [Fact]
    public async Task Export_recent_prices()
    {
        var coins = new List<string>()
        {
            "bitcoin", "pax-gold", "usd-coin", "ethereum", "binancecoin", "crypto-com-chain", "ftx-token",
            "huobi-token", "kucoin-shares", "okb", "woo-network", "1inch", "pancakeswap-token", "curve-dao-token",
            "dydx", "loopring", "thorchain", "havven", "sushi", "uniswap", "0x", "api3", "perpetual-protocol",
            "yearn-finance", "cosmos", "polkadot", "zelcash", "icon", "chainlink", "quant-network", "zencash",
            "aave", "anchor-protocol", "compound-governance-token", "kava", "maker", "cardano", "avalanche-2",
            "dogecoin", "terra-luna", "solana", "ripple", "amp-token", "the-graph", "axie-infinity", "chiliz",
            "enjincoin", "flow", "gala", "decentraland", "the-sandbox", "smooth-love-potion", "theta-token", "wax",
            "matic-network", "tron"
        };

        var data = await _simpleClient.PriceAsync(string.Join(",", coins), "usd");

        data.Should().NotBeNull();
        data.Result.Should().NotBeNullOrEmpty();

        _output.WriteLine("\"coin\",\"price\"");
        foreach (var coin in coins)
        {
            var price = data.Result[coin]["usd"]!.Value;
            _output.WriteLine($"\"{coin}\",\"{price}\"");
        }
    }
}
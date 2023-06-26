using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Trakx.CoinGecko.ApiClient.Models;
using Trakx.Common.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class CoinGeckoClientTests : CoinGeckoClientTestBase
{
    private readonly ICoinGeckoClient _coinsClient;
    private readonly string _quoteCurrencyId;
    private readonly DateTime _asOf;

    public CoinGeckoClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
        : base(apiFixture, output)
    {
        _coinsClient = ServiceProvider.GetRequiredService<ICoinGeckoClient>();

        _quoteCurrencyId = Constants.UsdCoin;
        _asOf = DateTime.Today.AddDays(-5);
    }

    [Theory]
    [ClassData(typeof(CoinGeckoIdsTestData))]
    public async Task GetLatestPrice_should_return_valid_price_when_passing_valid_id(string id)
    {
        var result = await _coinsClient.GetLatestPrice(id, Constants.Usd);
        result.Should().NotBeNull();
    }

    [Theory]
    [ClassData(typeof(CoinGeckoIdsTestData))]
    public async Task GetMarketDataAsOfFromId_should_return_valid_data_when_passing_valid_id(string id)
    {
        var result = await _coinsClient.GetMarketDataAsOfFromId(id, _asOf, _quoteCurrencyId);
        result!.AsOf.Should().NotBeNull();
        result.CoinId.Should().Be(id);
        result.CoinSymbol.Should().NotBeEmpty();
        result.MarketCap.Should().NotBeNull();
        result.Price.Should().BeGreaterThan(0);
        result.Volume.Should().BeGreaterThan(0);
        result.QuoteCurrency.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCoinGeckoIdFromSymbol_should_return_valid_data_when_passing_valid_id()
    {
        var result = await _coinsClient.GetCoinGeckoIdFromSymbol("btc");
        result.Should().Be("bitcoin");
    }

    [Fact]
    public async Task GetCoinList_should_return_the_full_list_when_passing_no_arguments()
    {
        var result = await _coinsClient.GetCoinList();
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSupportedQuoteCurrencies_includes_the_main_quote_currency()
    {
        var result = await _coinsClient.GetSupportedQuoteCurrencies();
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain(CoinGeckoClient.MainQuoteCurrency);
    }

    [Fact]
    public async Task GetAllPrices_returns_multiple_prices_for_valid_ids_and_currencies()
    {
        var baseIds = GetCoinIds();

        var quoteIds = CoinGeckoClient.MainQuoteCurrency.AsSingletonArray();

        var result = await _coinsClient.GetAllPrices(baseIds, quoteIds);

        AssertMultiplePrices(result, baseIds, quoteIds, quoteIds);
    }

    [Fact]
    public async Task GetAllPrices_returns_prices_for_a_very_long_list_of_tokens()
    {
        var baseIds = LotsOfTokens;

        var quoteIds = CoinGeckoClient.MainQuoteCurrency.AsSingletonArray();

        var result = await _coinsClient.GetAllPrices(baseIds, quoteIds);

        AssertMultiplePrices(result, baseIds, quoteIds, quoteIds);
    }

    [Fact]
    public async Task GetAllPrices_returns_multiple_prices_for_quote_currencies_even_if_not_supported_by_api()
    {
        var baseIds = GetCoinIds();

        const string unsupportedQuote = Constants.UsdCoin;
        var unsupportedQuoteIds = unsupportedQuote.AsSingletonArray();

        var supportedQuoteCurrencies = await _coinsClient.GetSupportedQuoteCurrencies();

        // sanity check
        supportedQuoteCurrencies.Should().NotContain(unsupportedQuote);

        var result = await _coinsClient.GetAllPrices(baseIds, unsupportedQuoteIds);

        AssertMultiplePrices(result, baseIds, unsupportedQuoteIds, supportedQuoteCurrencies);
    }

    [Fact]
    public async Task GetAllPricesExtended_should_return_marketCap_and_dailyVolume()
    {
        var coinIds = GetCoinIds();
        var quoteCurrencies = new[] { Constants.Usd, "eth" };

        var result = await _coinsClient.GetAllPricesExtended(
            coinIds,
            quoteCurrencies,
            includeMarketCap: true,
            include24HrVol: true);

        foreach (var coinId in coinIds)
        {
            foreach (var quoteCurrency in quoteCurrencies)
            {
                result.Should().Contain(p => p.CoinGeckoId == coinId && p.Currency == quoteCurrency);
            }
        }

        result.Should().OnlyContain(p => p.MarketCap > 0 && p.DailyVolume > 0);
        result.Should().HaveCount(coinIds.Length * quoteCurrencies.Length);
    }

    [Fact]
    public async Task GetMarketData_should_return_valid_data_when_passing_valid_id()
    {
        var coinGeckoId = "bitcoin";
        var currencyId = Constants.Usd;
        int daysCount = 2;
        var result = await _coinsClient.GetMarketData(coinGeckoId,
            currencyId, daysCount, CancellationToken.None);
        result.Should().HaveCountGreaterOrEqualTo(daysCount);
        foreach (var item in result)
        {
            item.Value.AsOf.Should().NotBeNull();
            item.Value.CoinId.Should().Be(coinGeckoId);
            item.Value.MarketCap.Should().NotBeNull();
            item.Value.Price.Should().BeGreaterThan(0);
            item.Value.Volume.Should().BeGreaterThan(0);
            item.Value.QuoteCurrency.Should().Be(currencyId);
        }
    }

    /// <summary>
    /// Asserts the prices collected from <see cref="SimpleClient.PriceAsync"/>
    /// and saved in a <see cref="MultiplePrices"/> result.
    /// </summary>
    internal static void AssertMultiplePrices(MultiplePrices result,
        string[] baseIds,
        string[] quoteIds,
        IEnumerable<string> supportedQuoteCurrencies)
    {
        var baseCount = baseIds.Length;
        var unsupportedCount = quoteIds.Except(supportedQuoteCurrencies).Count();

        var expectedBaseCount = baseCount + unsupportedCount;

        /// when called with no supported currencies,
        /// the client should add the requested quote ids to the base ids,
        /// then query the prices against the fallback main quote currency
        /// which is <see cref="CoinGeckoClient.MainQuoteCurrency"/>
        var expectedQuoteCount = Math.Max(1, quoteIds.Length - unsupportedCount);

        var expectedPriceCount = expectedBaseCount * expectedQuoteCount;
        result.TotalPriceCount.Should().Be(expectedPriceCount);

        foreach (var baseId in baseIds)
        {
            foreach (var quoteId in quoteIds)
            {
                var price = result.GetPrice(baseId, quoteId);
                price.Should().BeGreaterThan(0);
            }
        }
    }

    private static string[] GetCoinIds()
    {
        var ids = new CoinGeckoIdsTestData()
            .SelectMany(s => s!)
            .Select(s => s.ToString()!)
            .ToArray();
        return ids;
    }

    private static readonly string[] LotsOfTokens = new[]
    {
        "0x",
        "1inch",
        "aave",
        "access-protocol",
        "aelf",
        "akash-network",
        "alchemix-usd",
        "alchemy-pay",
        "aleph-zero",
        "algorand",
        "amp-token",
        "ankr",
        "apecoin",
        "aptos",
        "aragon",
        "arbitrum",
        "arweave",
        "astar",
        "astrafer",
        "audius",
        "avalanche-2",
        "axelar",
        "axie-infinity",
        "baby-doge-coin",
        "balancer",
        "band-protocol",
        "basic-attention-token",
        "beldex",
        "biconomy",
        "binancecoin",
        "binance-usd",
        "bitcoin",
        "bitcoin-avalanche-bridged-btc-b",
        "bitcoin-cash",
        "bitcoin-cash-sv",
        "bitcoin-gold",
        "bitdao",
        "bitget-token",
        "bitkub-coin",
        "bitrise-token",
        "bittorrent",
        "blockstack",
        "blox",
        "blur",
        "bone-shibaswap",
        "bora",
        "btse-token",
        "cardano",
        "cartesi",
        "casper-network",
        "cdai",
        "celo",
        "chainlink",
        "chia",
        "chiliz",
        "coinex-token",
        "compound-ether",
        "compound-governance-token",
        "compound-usd-coin",
        "compound-usdt",
        "conflux-token",
        "constellation-labs",
        "convex-crv",
        "convex-finance",
        "coredaoorg",
        "cosmos",
        "crypto-com-chain",
        "curve-dao-token",
        "dai",
        "dao-maker",
        "dash",
        "decentraland",
        "decred",
        "defichain",
        "digibyte",
        "dogecoin",
        "dydx",
        "ecash",
        "ecomi",
        "edu-coin",
        "elrond-erd-2",
        "energy-web-token",
        "enjincoin",
        "eos",
        "e-radix",
        "escoin-token",
        "ethereum",
        "ethereum-classic",
        "ethereum-name-service",
        "ethereum-pow-iou",
        "everscale",
        "fantom",
        "fetch-ai",
        "filecoin",
        "flare-networks",
        "flex-coin",
        "floki",
        "flow",
        "frax",
        "frax-ether",
        "frax-share",
        "gains-farm",
        "gains-network",
        "gala",
        "gatechain-token",
        "gemini-dollar",
        "gmx",
        "gnosis",
        "golem",
        "harmony",
        "havven",
        "hedera-hashgraph",
        "helium",
        "hive",
        "holotoken",
        "huobi-btc",
        "huobi-token",
        "icon",
        "iexec-rlc",
        "illuvium",
        "immutable-x",
        "injective-protocol",
        "insure",
        "internet-computer",
        "iostoken",
        "iota",
        "iotex",
        "jasmycoin",
        "joe",
        "just",
        "kadena",
        "kaspa",
        "kava",
        "klay-token",
        "krypton-dao",
        "kucoin-shares",
        "kusama",
        "kyber-network-crystal",
        "leo-token",
        "lido-dao",
        "link",
        "liquity-usd",
        "lisk",
        "litecoin",
        "livepeer",
        "loopring",
        "lukso-token",
        "magic",
        "maker",
        "mask-network",
        "matic-network",
        "metars-genesis",
        "mina-protocol",
        "monero",
        "moonbeam",
        "msol",
        "mx-token",
        "near",
        "nem",
        "neo",
        "nervos-network",
        "nexo",
        "nucypher",
        "nxm",
        "oasis-network",
        "ocean-protocol",
        "oec-token",
        "okb",
        "olympus",
        "ontology",
        "optimism",
        "ordinals",
        "osmosis",
        "pancakeswap-token",
        "pax-gold",
        "paxos-standard",
        "pepe",
        "polkadot",
        "polymath",
        "polymesh",
        "qtum",
        "quant-network",
        "radix",
        "ravencoin",
        "render-token",
        "reserve-rights-token",
        "ribbon-finance",
        "ripple",
        "rocket-pool",
        "rocket-pool-eth",
        "ronin",
        "safemoon",
        "safepal",
        "seth2",
        "shiba-inu",
        "siacoin",
        "singularitynet",
        "skale",
        "solana",
        "ssv-network",
        "staked-ether",
        "staked-frax-ether",
        "stargate-finance",
        "stasis-eurs",
        "stellar",
        "stepn",
        "sui",
        "sushi",
        "swipe",
        "swissborg",
        "synapse-2",
        "telcoin",
        "terra-luna",
        "terra-luna-2",
        "terrausd",
        "tether",
        "tether-eurt",
        "tether-gold",
        "tezos",
        "the-graph",
        "the-open-network",
        "the-sandbox",
        "theta-fuel",
        "theta-token",
        "thorchain",
        "tokenize-xchange",
        "tominet",
        "tomochain",
        "tribe-2",
        "tron",
        "true-usd",
        "trust-wallet-token",
        "uma",
        "uniswap",
        "usd-coin",
        "usdd",
        "utility-web3shot",
        "vechain",
        "waves",
        "wax",
        "wemix-token",
        "whitebit",
        "woo-network",
        "wrapped-beacon-eth",
        "wrapped-bitcoin",
        "xdce-crowd-sale",
        "yearn-finance",
        "zcash",
        "zelcash",
        "zilliqa",
    };
}
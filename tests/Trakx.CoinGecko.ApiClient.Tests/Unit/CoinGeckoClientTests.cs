using Microsoft.Extensions.Caching.Memory;
using Trakx.Common.ApiClient.Extensions;
using Trakx.Common.Testing.Mocks;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public partial class CoinGeckoClientTests
{
    private const string VsCurrency = Constants.Usd;

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

        var timestamp = (date ?? _mockCreator.GetUtcDateTime()).ToDateString();

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
            vs_currency: ICoinGeckoMarketClient.MainQuoteCurrency,
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
                new() { dates[0], 318_992_245_176.35913 },
                new() { dates[1], 319_632_242_563.95764 },
            },
            Total_volumes = new List<TimestampedValue>
            {
                new() { dates[0], 38_069_451_649.54143 },
                new() { dates[1], 38_825_217_290.29339 },
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

    private void AssertCachedEntry(params object?[] keyFragments)
    {
        AssertCachedEntryBase(nameof(_memoryCache.CreateEntry), keyFragments);
    }

    private void AssertReusedCachedEntry(params object?[] keyFragments)
    {
        AssertCachedEntryBase(nameof(_memoryCache.TryGetValue), keyFragments);
    }

    private void AssertCachedEntryBase(string methodName, object?[] keyFragments)
    {
        string[] fragments = keyFragments
            .Where(p => p != null)
            .Select(p => p!.ToString()!)
            .ToArray();

        string[] cacheKeys = _memoryCache
            .GetReceivedCalls(methodName)
            .Select(p => p.GetArgument<object>())
            .Where(p => p != null)
            .Select(p => p.ToString()!)
            .ToArray();

        foreach (var cacheKey in cacheKeys)
        {
            var isExpectedKey = fragments.All(cacheKey.Contains);
            if (isExpectedKey) return; // we found a key with all expected fragments
        }

        string StringsToList(string[] s) => string.Join(Environment.NewLine + " - ", s.Prepend(" "));

        // we did not find a key matching all fragments
        var failMessage = $"""
            No entry found in MemoryCache with key fragments:{StringsToList(fragments)}

            Found cached entries with the following keys:{StringsToList(cacheKeys)}
            """;

        Assert.Fail(failMessage);
    }
}

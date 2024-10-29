using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Trakx.Common.ApiClient.Extensions;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public partial class CoinGeckoClientTests
{
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

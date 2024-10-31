using System.Globalization;
using Microsoft.Extensions.Logging;
using Trakx.Common.Extensions;

namespace Trakx.CoinGecko.ApiClient;

// will soon become CoinGeckoMarketClient
public partial class CoinGeckoClient
{
    private readonly ICoinsClient _coinsClient;

    private async Task<Dictionary<DateTimeOffset, MarketData>> GetMarketDataInternal(
        string id,
        string vsCurrency,
        int days,
        CancellationToken cancellationToken = default)
    {
        var daysString = days.ToString(CultureInfo.InvariantCulture);
        var range = await _coinsClient.Market_chartAsync(id, vsCurrency, daysString, "daily", cancellationToken: cancellationToken);
        return BuildMarketData(id, vsCurrency, range.Content);
    }

    private async Task<Dictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRangeInternal(
        string id,
        string vsCurrency,
        long startUnix,
        long endUnix,
        CancellationToken cancellationToken = default)
    {
        var range = await _coinsClient.RangeAsync(id, vsCurrency, startUnix, endUnix, cancellationToken);
        return BuildMarketData(id, vsCurrency, range.Content);
    }

    private async Task<MarketData?> GetMarketDataAsOfFromIdInternal(
        string id,
        DateTime asOf,
        string quoteCurrencyId,
        string date,
        CancellationToken cancellationToken = default)
    {
        var fullData = await _coinsClient.HistoryAsync(id, date, localization: false, cancellationToken);

        var content = fullData.Content;
        var data = content.Market_data;
        if (data == null) return null;

        var fxRate = await GetUsdFxRate(quoteCurrencyId, date, cancellationToken);

        return new MarketData
        {
            AsOf = asOf,

            CoinId = content.Id,
            CoinSymbol = content.Symbol,
            QuoteCurrency = content.Symbol,

            MarketCap = data.Market_cap[MainQuoteCurrency] / fxRate,
            Volume = data.Total_volume[MainQuoteCurrency] / fxRate,
            Price = data.Current_price[MainQuoteCurrency] / fxRate,
        };
    }

    private async Task<List<MarketData>> GetMarketRankInternal(int limit, CancellationToken cancellationToken = default)
    {
        var result = new List<MarketData>();

        int pageSize = Math.Min(250, limit);
        int pageCount = (limit + pageSize - 1) / pageSize;

        for (int page = 1; page <= pageCount; page++)
        {
            var partialResult = await Search(page: page, per_page: pageSize, cancellationToken: cancellationToken);

            if (partialResult.IsNullOrEmpty())
                return result;

            result.AddRange(partialResult);
        }

        return result;
    }

    private async Task<List<MarketData>> SearchMarketsInternal(
        string vsCurrency = ICoinGeckoMarketClient.MainQuoteCurrency,
        string? ids = null,
        string? category = null,
        string order = ICoinGeckoMarketClient.DefaultSearchOrder,
        int? per_page = null,
        int? page = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _coinsClient.MarketsAsync(vsCurrency, ids, category, order, per_page, page, cancellationToken: cancellationToken);
        var coins = result?.Content ?? [];

        return coins.ConvertAll(x => new MarketData
        {
            CoinId = x.Id,
            Name = x.Name,
            MarketCap = x.Market_cap,
            Price = x.Current_price,
            CoinSymbol = x.Symbol,
            Volume = x.Total_volume,
            CirculatingSupply = x.Circulating_supply,
            MarketCapRank = x.Market_cap_rank,
        });
    }

    internal async Task<decimal> GetUsdFxRate(string quoteCurrencyId, string date, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quoteCurrencyId);
        return await GetFromCacheOrApi(
            [nameof(GetUsdFxRate), quoteCurrencyId, date],
            async () => await GetUsdFxRateInternal(quoteCurrencyId, date, cancellationToken));
    }

    private async Task<decimal> GetUsdFxRateInternal(
        string quoteCurrencyId,
        string date,
        CancellationToken cancellationToken = default)
    {
        var quoteResponse = await _coinsClient.HistoryAsync(quoteCurrencyId, date, localization: false, cancellationToken);

        decimal? fxRate = null;
        var currentPrice = quoteResponse.Content.Market_data?.Current_price;
        currentPrice?.TryGetValue(MainQuoteCurrency, out fxRate);

        if (fxRate != null) return fxRate.Value;

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug(
                "Current price for '{quoteCurrency}' in coin id '{quoteCurrencyId}' for '{date}' is missing.",
                MainQuoteCurrency, quoteCurrencyId, date);
        }

        throw new FailedToRetrievePriceException($"Failed to retrieve price of {quoteCurrencyId} as of {date}");
    }

    private static Dictionary<DateTimeOffset, MarketData> BuildMarketData(string id, string vsCurrency, Range range)
    {
        return Enumerable
            .Range(0, range.Prices.Count)
            .Select(i => new
            {
                Index = i,
                Date = range.Prices[i][0].AsUnixMillisToDate(),
            })
            .ToDictionary(
                d => d.Date,
                d => new MarketData
                {
                    AsOf = d.Date,
                    CoinId = id,
                    CoinSymbol = null,
                    MarketCap = (decimal)range.Market_caps[d.Index][1],
                    Price = (decimal)range.Prices[d.Index][1],
                    Volume = (decimal)range.Total_volumes[d.Index][1],
                    QuoteCurrency = vsCurrency
                });
    }
}

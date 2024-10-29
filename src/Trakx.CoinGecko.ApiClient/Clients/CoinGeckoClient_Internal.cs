using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Trakx.Common.ApiClient;
using Trakx.Common.Extensions;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public partial class CoinGeckoClient
{
    private static readonly TimeSpan DefaultCacheLifeSpan = TimeSpan.FromDays(1);

    private async Task<string?> GetCoinGeckoIdFromSymbolInternal(string symbol, CancellationToken cancellationToken)
    {
        var map = await GetSymbolToCoinGeckoIdMap(cancellationToken);
        var id = map.GetValueOrDefault(symbol)?.FirstOrDefault();
        if (id != null) return id;

        // TODO: search the API for tokens with the symbol
        return null;
    }

    private async Task<SymbolToCoinGeckoIdsMap> GetSymbolToCoinGeckoIdMapInternal(CancellationToken cancellationToken)
    {
        // get the highest ranked coin
        var rank = await GetMarketRank(cancellationToken: cancellationToken);

        // rank coin symbols, having the most popular option at the top
        // example: there's a UNI token at #28 market cap and another with rank ~2300
        // there's a really, really, really high chance we want the most popular token
        var rankLookup = rank
            .Where(p => p.CoinSymbol != null && p.CoinId != null)
            .ToLookup(p => p.CoinSymbol!, StringComparer.OrdinalIgnoreCase);

        var bestCandidates = rankLookup.ToDictionary(
            group => group.Key,
            group => group
                .OrderBy(p => p.MarketCapRank ?? int.MaxValue)
                .Select(p => p.CoinId!).ToArray());

        return bestCandidates;
    }

    private async Task<List<CoinList>> GetCoinListFromApi(CancellationToken cancellationToken)
    {
        var coinList = await _coinsClient.ListAllAsync(cancellationToken: cancellationToken);
        return coinList.Content;
    }

    private async Task<HashSet<string>> GetSupportedQuoteCurrenciesFromApi(CancellationToken cancellationToken)
    {
        var response = await _simpleClient.Supported_vs_currenciesAsync(cancellationToken);
        var result = new HashSet<string>(response.Content, StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private async Task<Dictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRangeFromApi(
        string id, string vsCurrency, long startUnix, long endUnix, CancellationToken cancellationToken)
    {
        var range = await _coinsClient.RangeAsync(id, vsCurrency, startUnix, endUnix, cancellationToken);
        return BuildMarketData(id, vsCurrency, range.Content);
    }

    private async Task<Dictionary<DateTimeOffset, MarketData>> GetMarketDataFromApi(
        string id, string vsCurrency, int days,
        CancellationToken cancellationToken)
    {
        var daysString = days.ToString(CultureInfo.InvariantCulture);
        var range = await _coinsClient.Market_chartAsync(id, vsCurrency, daysString, "daily", cancellationToken: cancellationToken);
        return BuildMarketData(id, vsCurrency, range.Content);
    }

    private async Task<List<MarketData>> SearchApi(
        string vsCurrency = Constants.Usd,
        string? ids = null,
        string? category = null,
        string order = "market_cap_desc",
        int? per_page = null,
        int? page = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _coinsClient.MarketsAsync(vsCurrency, ids, category, order, per_page, page, cancellationToken: cancellationToken);

        if (result == null) return new();
        if (result.Content.IsNullOrEmpty()) return new();

        return result.Content.ConvertAll(x => new MarketData
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

    private async Task<MarketData?> GetMarketDataAsOfFromIdFromApi(string id, DateTime asOf, string quoteCurrencyId, string date)
    {
        var fullData = await _coinsClient.HistoryAsync(id, date, localization: false);

        var content = fullData.Content;
        var data = content.Market_data;
        if (data == null) return null;

        var fxRate = await GetUsdFxRate(quoteCurrencyId, date);

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

    private async Task<List<MarketData>> GetMarketRankFromApi(int limit, CancellationToken cancellationToken)
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

    internal static string GetDateString(DateTime date)
    {
        return date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    private static Dictionary<DateTimeOffset, MarketData> BuildMarketData(string id, string vsCurrency, Range range)
    {
        return Enumerable
            .Range(0, range.Prices.Count)
            .Select(i => new { Index = i, Date = DateTimeOffset.FromUnixTimeMilliseconds((long)range.Prices[i][0]) })
            .ToDictionary(d => d.Date,
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

    private async Task<decimal> GetUsdFxRate(string quoteCurrencyId, string date)
    {
        if (quoteCurrencyId.IsNullOrWhiteSpace())
            throw new ArgumentException($"{nameof(quoteCurrencyId)} can not be null or whitespace",
                nameof(quoteCurrencyId));

        var cacheKey = $"{_typeName}|usd-fx-rate|{quoteCurrencyId}|{date}";
        return await GetFromCacheOrApi(cacheKey, async () => await GetUsdFxRateFromApi(quoteCurrencyId, date));
    }

    private async Task<decimal> GetUsdFxRateFromApi(string quoteCurrencyId, string date)
    {
        var quoteResponse = await _coinsClient.HistoryAsync(quoteCurrencyId, date, localization: false);

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

    private async Task<T> GetFromCacheOrApi<T>(string cacheKey, Func<Task<T>> getFromApi)
    {
        var value = await _cache.GetOrCreateAsync<T>(cacheKey, async (entry) =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheLifeSpan;
            return await getFromApi();
        });

        return value!;
    }

    private async Task<Response<IDictionary<string, IDictionary<string, decimal?>>>> GetAllPricesInternal(
        IEnumerable<string> ids,
        string[]? vsCurrencies,
        CancellationToken cancellationToken)
    {
        (var baseIds, var quoteIds) = await GetIdsForPriceQuery(ids, vsCurrencies, cancellationToken);

        var response = await _simpleClient.PriceAsync(baseIds, quoteIds, cancellationToken: cancellationToken);

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Received latest price {PriceAsyncResponse}", JsonSerializer.Serialize(response));
        }

        return response;
    }

    /// <summary>
    /// Each requested quote currency needs to be either a 'base' or a 'vs' id in the price call,
    /// depending if it's a supported quote currency or not.<br />
    /// This method ensures a valid list of 'base' and 'vs' ids
    /// according to the logic explained in the comment for <see cref="GetLatestPrice(string, string)"/>
    /// </summary>
    private async Task<(string BaseIds, string QuoteIds)> GetIdsForPriceQuery(
        IEnumerable<string> ids,
        string[]? vsCurrencies,
        CancellationToken cancellationToken)
    {
        List<string> baseIds = new();
        List<string> quoteIds = new();

        if (ids != null) baseIds.AddRange(ids);

        vsCurrencies ??= [];

        var supportedQuoteCurrencies = await GetSupportedQuoteCurrencies(cancellationToken);

        foreach (var id in vsCurrencies)
        {
            var isSupported = supportedQuoteCurrencies.Contains(id);
            if (isSupported) quoteIds.Add(id);
            else baseIds.Add(id);
        }

        // ensure at least one supported quote currency
        if (quoteIds.Count == 0)
        {
            quoteIds.Add(MainQuoteCurrency);
        }

        var baseList = baseIds.ToCsvList(distinct: true, toLower: true, quoted: false);
        var quoteList = quoteIds.ToCsvList(distinct: true, toLower: true, quoted: false);
        return (baseList, quoteList);
    }
}

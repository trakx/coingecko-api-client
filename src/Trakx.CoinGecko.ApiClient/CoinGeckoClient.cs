﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Polly;
using Polly.Retry;
using Serilog;
using Trakx.Utils.Extensions;

namespace Trakx.CoinGecko.ApiClient
{
    public class CoinGeckoClient : ICoinGeckoClient
    {
        private static readonly ILogger Logger =
            Log.Logger.ForContext(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly ICoinsClient _coinsClient;
        private readonly ISimpleClient _simpleClient;
        private Dictionary<string, string>? _symbolsByNames;
        private Dictionary<string, string>? _idsBySymbolName;
        private readonly  AsyncRetryPolicy _retry;

        public Dictionary<string, string> IdsBySymbolName
        {
            get
            {
                var idsBySymbolName = _idsBySymbolName ??= GetCoinList().GetAwaiter().GetResult()
                    .ToDictionary(c => GetSymbolNameKey(c.Symbol, c.Name), c => c.Id);
                return idsBySymbolName;
            }
        }

        public Dictionary<string, CoinFullData> CoinFullDataByIds { get; }

        public CoinGeckoClient(ICoinsClient coinsClient, ISimpleClient simpleClient)
        {
            _coinsClient = coinsClient;
            _simpleClient = simpleClient;

            CoinFullDataByIds = new Dictionary<string, CoinFullData>();
            _retry = Policy
                .Handle<ApiException>()
                .WaitAndRetryForeverAsync((i, exception, arg3) =>
                {
                    if(exception is ApiException apiException
                       && apiException.StatusCode == (int)HttpStatusCode.TooManyRequests
                       && apiException.Headers.TryGetValue("Retry-After", out var value))
                    {
                        var millisecondsDelay = int.Parse(value?.First() ?? "0"); 
                        return TimeSpan.FromSeconds(millisecondsDelay);
                    } 
                    return TimeSpan.Zero;
                }, (exception, i, timeSpan, context) =>
                {
                    Logger.Warning(exception, "Failed to retrieve CoingGecko results, retrying in {timeSpan}",
                        timeSpan);
                    return Task.CompletedTask;
                });
        }
        
        /// <inheritdoc />
        public async Task<decimal?> GetLatestPrice(string coinGeckoId, string quoteCurrencyId = "usd-coin")
        {
            Guard.Against.NullOrEmpty(quoteCurrencyId, nameof(quoteCurrencyId));

            var tickerDetails = (await _retry.ExecuteAndCaptureAsync(async () => 
                        await _simpleClient
                        .PriceAsync($"{coinGeckoId},{quoteCurrencyId}", "usd").ConfigureAwait(false))
                    .ConfigureAwait(false)).Result;

            Logger.Debug(JsonSerializer.Serialize(tickerDetails));

            var price = tickerDetails.Result[coinGeckoId]["usd"];
            var conversionToQuoteCurrency = quoteCurrencyId == "usd"
                ? 1m
                : tickerDetails.Result[quoteCurrencyId]["usd"];

            return price / conversionToQuoteCurrency ?? 0m;
        }

        public async Task<string?> GetCoinGeckoIdFromSymbol(string symbol)
        {
            var coinList = await GetCoinList();

            var ids = coinList.Where(c =>
                c.Symbol.Equals(symbol, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
            return ids.Count == 1 ? ids[0].Id : null;
        }

        /// <inheritdoc />
        public async Task<decimal?> GetPriceAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = Constants.UsdCoin)
        {
            try
            {
                var date = asOf.ToString("dd-MM-yyyy");

                var fxRate = await GetUsdFxRate(quoteCurrencyId, date);

                var historicalPrice = (await _retry.ExecuteAndCaptureAsync(async () => 
                    await _coinsClient.HistoryAsync(id, date, false.ToString()).ConfigureAwait(false))
                    .ConfigureAwait(false)).Result;

                return historicalPrice.Result.Market_data.Current_price[Constants.Usd] / fxRate;
            }
            catch (Exception e)
            {
                Logger.Debug(e, "Failed to retrieve price for {0} as of {1:yyyyMMdd}", id, asOf);
                return null;
            }
        }

        private async Task<decimal> GetUsdFxRate(string quoteCurrencyId, string date)
        {
            Guard.Against.NullOrWhiteSpace(quoteCurrencyId, nameof(quoteCurrencyId));

            var quoteResponse = (await _retry.ExecuteAndCaptureAsync(async () => 
                    await _coinsClient.HistoryAsync(quoteCurrencyId, date, false.ToString())
                    .ConfigureAwait(false))
                .ConfigureAwait(false)).Result;

            var fxRate = quoteResponse.Result.Market_data.Current_price.ContainsKey(Constants.Usd) ?
                quoteResponse.Result.Market_data.Current_price[Constants.Usd] : default;

            if (fxRate != null) return (decimal) fxRate;

            Logger.Debug($"Current price for '{Constants.Usd}' in coin id '{quoteCurrencyId} for date '{date:dd-MM-yyyy}' is missing.");
            throw new FailedToRetrievePriceException($"Failed to retrieve price of {quoteCurrencyId} as of {date}");

        }

        /// <inheritdoc />
        public async Task<MarketData> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin")
        {
            var date = asOf.ToString("dd-MM-yyyy");
            var fullData = (await _retry.ExecuteAndCaptureAsync(async () => 
                await _coinsClient.HistoryAsync(id, date, false.ToString())
                .ConfigureAwait(false))
                .ConfigureAwait(false)).Result;
            var fxRate = await GetUsdFxRate(quoteCurrencyId, date);
            var marketData = new MarketData
            {
                AsOf = asOf,
                CoinId = fullData.Result.Id,
                CoinSymbol = fullData.Result.Symbol,
                MarketCap = fullData.Result.Market_data.Market_cap[Constants.Usd] / fxRate,
                Volume = fullData.Result.Market_data.Total_volume[Constants.Usd] / fxRate,
                Price = fullData.Result.Market_data.Current_price[Constants.Usd] / fxRate,
                QuoteCurrency = fullData.Result.Symbol
            };

            return marketData;
        }

        public bool TryRetrieveSymbol(string coinName, out string? symbol)
        {
            _symbolsByNames ??= GetCoinList().GetAwaiter().GetResult().ToDictionary(c => c.Name, c => c.Symbol);
            var bestMatch = coinName.FindBestLevenshteinMatch(_symbolsByNames.Keys);
            symbol = bestMatch != null ? _symbolsByNames[bestMatch] : null;

            return bestMatch != null;
        }

        private string GetSymbolNameKey(string symbol, string name)
        {
            return $"{symbol.ToLower()}|{name.ToLower()}";
        }

        public string RetrieveCoinGeckoId(string symbol, string name)
        {
            var symbolNameKey = GetSymbolNameKey(symbol, name);
            if (IdsBySymbolName.TryGetValue(symbolNameKey, out var coinGeckoId))
                return coinGeckoId;

            var bestMatch = symbolNameKey.FindBestLevenshteinMatch(IdsBySymbolName.Keys);
            if (bestMatch == null) throw new FailedToRetrieveCoinGeckoIdException($"Unable to find the best coin gecko id match for symbol '{symbol}'.");

            coinGeckoId = IdsBySymbolName[bestMatch];
            IdsBySymbolName.Add(symbolNameKey, coinGeckoId);
            return coinGeckoId;

        }
        public CoinFullData? RetrieveCoinFullData(string coinId)
        {
            if (CoinFullDataByIds.TryGetValue(coinId, out var data)) return data;

            try
            {
                data = _retry.ExecuteAndCaptureAsync(async () => 
                        await _coinsClient.CoinsAsync(coinId, "false",
                            false, false, false, false, false))
                    .GetAwaiter().GetResult().Result.Result;

                CoinFullDataByIds[coinId] = data;
                return data;
            }
            catch (Exception exception)
            {
                Logger.Warning(exception, "Failed to retrieve coin data for {0}", coinId);
                return default;
            }
        }

        public async Task<IReadOnlyList<CoinList>> GetCoinList()
        {
            var coinList = (await _retry.ExecuteAndCaptureAsync(async () =>
                    await _coinsClient.ListAllAsync().ConfigureAwait(false))
                .ConfigureAwait(false)).Result;
                
                return coinList.Result;
        }

        public async Task<IDictionary<string, IDictionary<string, decimal?>>> GetAllPrices(string[] ids, string[]? vsCurrencies = null)
        {
            var coinsPrice = (await _retry.ExecuteAndCaptureAsync(async () =>
                    await _simpleClient.PriceAsync(ids.ToCsvList(true, true, quoted: false),
                            (vsCurrencies ?? new[] {Constants.Usd}).ToCsvList(true, true, quoted: false))
                        .ConfigureAwait(false))
                .ConfigureAwait(false)).Result;
            return coinsPrice.Result;
        }

        public async Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(string id, string vsCurrency, DateTimeOffset start, DateTimeOffset end,
            CancellationToken cancellationToken)
        {
            var range = (await _retry.ExecuteAndCaptureAsync(async () => 
                await _coinsClient
                .RangeAsync(id, vsCurrency, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), CancellationToken.None)
                .ConfigureAwait(false))
                .ConfigureAwait(false)).Result;
            
            var result = Enumerable.Range(0, range.Result.Prices.Count).Select(i =>
                new { Index = i, Date = DateTimeOffset.FromUnixTimeMilliseconds((long)range.Result.Prices[i][0]) })
                .ToDictionary(d => d.Date,
                    d => new MarketData
                    {
                        AsOf = d.Date,
                        CoinId = id,
                        CoinSymbol = null,
                        MarketCap = (decimal)range.Result.Market_caps[d.Index][1],
                        Price = (decimal)range.Result.Prices[d.Index][1],
                        Volume = (decimal)range.Result.Total_volumes[d.Index][1],
                        QuoteCurrency = vsCurrency
                    });
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using CoinGecko.Entities.Response.Coins;
using CoinGecko.Entities.Response.Simple;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Trakx.CoinGecko.ApiClient.Pricing;
using Trakx.Utils.Extensions;

namespace Trakx.CoinGecko.ApiClient
{
    public class CoinGeckoClient : ICoinGeckoClient
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CoinGeckoClient> _logger;
        private readonly ICoinsClient _coinsClient;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly ISimpleClient _simpleClient;
        private Dictionary<string, string>? _symbolsByNames;
        private Dictionary<string, string>? _idsBySymbolName;

        public Dictionary<string, string> IdsBySymbolName
        {
            get
            {
                var idsBySymbolName = _idsBySymbolName ??= CoinList
                    .ToDictionary(c => GetSymbolNameKey(c.Symbol, c.Name), c => c.Id);
                return idsBySymbolName;
            }
        }

        public Dictionary<string, CoinFullDataById> CoinFullDataByIds { get; }

        public CoinGeckoClient(ClientFactory factory, IMemoryCache memoryCache, ILogger<CoinGeckoClient> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            _retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, c => TimeSpan.FromSeconds(c * c));
            _coinsClient = factory.CreateCoinsClient();
            _simpleClient = factory.CreateSimpleClient();

            CoinFullDataByIds = new Dictionary<string, CoinFullDataById>();
        }

        /// <inheritdoc />
        public async Task<decimal?> GetLatestPrice(string coinGeckoId, string quoteCurrencyId = "usd-coin")
        {
            Guard.Against.NullOrEmpty(quoteCurrencyId, nameof(quoteCurrencyId));

            var tickerDetails = await _simpleClient
                .GetSimplePrice(new[] {coinGeckoId, quoteCurrencyId}, new[] {Constants.Usd})
                .ConfigureAwait(false);
            
            _logger.LogDebug(JsonSerializer.Serialize(tickerDetails));

            var price = tickerDetails[coinGeckoId][Constants.Usd];
            var conversionToQuoteCurrency = quoteCurrencyId == Constants.Usd 
                ? 1m 
                : tickerDetails[quoteCurrencyId][Constants.Usd];

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
        public async Task<decimal?> GetPriceAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin")
        {
            try
            {
                var date = asOf.ToString("dd-MM-yyyy");

                var fxRate = await GetUsdFxRate(quoteCurrencyId, date);

                var historicalPrice = await _retryPolicy.ExecuteAsync(() =>
                        _coinsClient.GetHistoryByCoinId(id, date, false.ToString()));

                return historicalPrice.MarketData.CurrentPrice[Constants.Usd] / fxRate;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to retrieve price for {0} as of {1:yyyyMMdd}", id, asOf);
                return null;
            }
        }

        private async Task<decimal> GetUsdFxRate(string quoteCurrencyId, string date)
        {
            Guard.Against.NullOrWhiteSpace(quoteCurrencyId, nameof(quoteCurrencyId));

            var quoteResponse = await _memoryCache.GetOrCreateAsync($"{date}|{quoteCurrencyId}",
                async entry => await _retryPolicy.ExecuteAsync(() =>
                    _coinsClient.GetHistoryByCoinId(quoteCurrencyId, date, false.ToString())));

            var fxRate = quoteResponse.MarketData.CurrentPrice[Constants.Usd];

            if (fxRate == null)
                throw new FailedToRetrievePriceException($"Failed to retrieve price of {quoteCurrencyId} as of {date}");

            return (decimal)fxRate;
        }

        /// <inheritdoc />
        public async Task<MarketData> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin")
        {
            var date = asOf.ToString("dd-MM-yyyy");
            var fullData = await _coinsClient.GetHistoryByCoinId(id, date, false.ToString())
                .ConfigureAwait(false);
            var fxRate = await GetUsdFxRate(quoteCurrencyId, date);
            var marketData = new MarketData
            {
                AsOf = asOf,
                CoinId = fullData.Id,
                CoinSymbol = fullData.Symbol,
                MarketCap = fullData.MarketData?.MarketCap[Constants.Usd] / fxRate,
                Volume = fullData.MarketData?.TotalVolume[Constants.Usd] / fxRate,
                Price = fullData.MarketData?.CurrentPrice[Constants.Usd] / fxRate,
                QuoteCurrency = fullData.Symbol
            };

            return marketData;
        }

        public bool TryRetrieveSymbol(string coinName, out string? symbol)
        {
            _symbolsByNames ??= CoinList.ToDictionary(c => c.Name, c => c.Symbol);
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
            coinGeckoId = IdsBySymbolName[bestMatch];
            IdsBySymbolName.Add(symbolNameKey, coinGeckoId);
            return coinGeckoId;
        }
        public CoinFullDataById? RetrieveCoinFullData(string coinId)
        {
            if (CoinFullDataByIds.TryGetValue(coinId, out var data)) return data;

            try
            {
                data = _coinsClient.GetAllCoinDataWithId(coinId, "false",
                false, false, false, false, false)
                    .GetAwaiter().GetResult();

                CoinFullDataByIds[coinId] = data;
                return data;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to retrieve coin data for {0}", coinId);
                return default;
            }
        }

        public async Task<IReadOnlyList<CoinList>> GetCoinList()
        {
            var coinList = await _memoryCache.GetOrCreateAsync("CoinGecko.CoinList", async entry =>
                await _retryPolicy.ExecuteAsync(() => _coinsClient.GetCoinList()).ConfigureAwait(false));
            return coinList;
        }

        public async Task<Price> GetAllPrices(string[] ids, string[]? vsCurrencies = null)
        {
            var coinsPrice = await _simpleClient.GetSimplePrice(ids, vsCurrencies ?? new[] { Constants.Usd }).ConfigureAwait(false);
            return coinsPrice;
        }

        public IReadOnlyList<CoinList> CoinList => _memoryCache.GetOrCreate("CoinGecko.CoinList",
            entry =>
            {
                var coinList = _retryPolicy.ExecuteAsync(() => _coinsClient.GetCoinList())
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
                return coinList;
            });
    }
}
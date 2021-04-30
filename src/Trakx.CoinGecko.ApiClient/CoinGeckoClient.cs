using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private readonly ILogger _logger;
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

        public Dictionary<string, CoinFullData> CoinFullDataByIds { get; }

        public CoinGeckoClient(IClientFactory factory, ILogger logger)
        {
            _logger = logger;
            _retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, c => TimeSpan.FromSeconds(c * c));
            _coinsClient = factory.CreateCoinsClient();
            _simpleClient = factory.CreateSimpleClient();

            CoinFullDataByIds = new Dictionary<string, CoinFullData>();
        }

        /// <inheritdoc />
        public async Task<decimal?> GetLatestPrice(string coinGeckoId, string quoteCurrencyId = "usd-coin")
        {
            Guard.Against.NullOrEmpty(quoteCurrencyId, nameof(quoteCurrencyId));

            var tickerDetails = await _simpleClient
                .PriceAsync($"{coinGeckoId},{quoteCurrencyId}", "usd")
                .ConfigureAwait(false);

            _logger.Debug(JsonSerializer.Serialize(tickerDetails));

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

                var historicalPrice = await _retryPolicy.ExecuteAsync(() =>
                        _coinsClient.HistoryAsync(id, date, false.ToString()));

                return historicalPrice.Result.Market_data.Current_price[Constants.Usd] / fxRate;
            }
            catch (Exception e)
            {
                _logger.Debug(e, "Failed to retrieve price for {0} as of {1:yyyyMMdd}", id, asOf);
                return null;
            }
        }

        private async Task<decimal> GetUsdFxRate(string quoteCurrencyId, string date)
        {
            Guard.Against.NullOrWhiteSpace(quoteCurrencyId, nameof(quoteCurrencyId));

            var quoteResponse = await _retryPolicy.ExecuteAsync(() =>
                _coinsClient.HistoryAsync(quoteCurrencyId, date, false.ToString()));

            var fxRate = quoteResponse.Result.Market_data.Current_price.ContainsKey(Constants.Usd) ?
                quoteResponse.Result.Market_data.Current_price[Constants.Usd] : default;

            if (fxRate == null)
            {
                _logger.Debug($"Current price for '{Constants.Usd}' in coin id '{quoteCurrencyId} for date '{date:dd-MM-yyyy}' is missing.");
                throw new FailedToRetrievePriceException($"Failed to retrieve price of {quoteCurrencyId} as of {date}");
            }

            return (decimal)fxRate;
        }

        /// <inheritdoc />
        public async Task<MarketData> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin")
        {
            var date = asOf.ToString("dd-MM-yyyy");
            var fullData = await _coinsClient.HistoryAsync(id, date, false.ToString())
                .ConfigureAwait(false);
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
            if (bestMatch != null)
            {
                coinGeckoId = IdsBySymbolName[bestMatch];
                IdsBySymbolName.Add(symbolNameKey, coinGeckoId);
                return coinGeckoId;
            }
            else
            {
                throw new FailedToRetrieveCoinGeckoIdException($"Unable to find the best coin gecko id match for symbol '{symbol}'.");
            }
        }
        public CoinFullData? RetrieveCoinFullData(string coinId)
        {
            if (CoinFullDataByIds.TryGetValue(coinId, out var data)) return data;

            try
            {
                data = _coinsClient.CoinsAsync(coinId, "false",
                        false, false, false, false, false)
                    .GetAwaiter().GetResult().Result;

                CoinFullDataByIds[coinId] = data;
                return data;
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Failed to retrieve coin data for {0}", coinId);
                return default;
            }
        }

        public async Task<IReadOnlyList<CoinList>> GetCoinList()
        {
            var coinList =
                await _retryPolicy.ExecuteAsync(() =>
                    _coinsClient.ListAllAsync()).ConfigureAwait(false);
            return coinList.Result;
        }

        public async Task<IDictionary<string, IDictionary<string, decimal?>>> GetAllPrices(string[] ids, string[]? vsCurrencies = null)
        {
            var coinsPrice = await _simpleClient.PriceAsync(string.Join(",", ids),
                string.Join(",", vsCurrencies ?? new[] { Constants.Usd })).ConfigureAwait(false);
            return coinsPrice.Result;
        }

        public IReadOnlyList<CoinList> CoinList => _retryPolicy.ExecuteAsync(() => _coinsClient.ListAllAsync())
            .ConfigureAwait(false).GetAwaiter().GetResult().Result;
    }
}
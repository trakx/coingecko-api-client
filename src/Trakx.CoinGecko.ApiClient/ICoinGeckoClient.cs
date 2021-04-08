using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Trakx.CoinGecko.ApiClient
{
    public interface ICoinGeckoClient
    {
        Task<decimal?> GetLatestPrice(string coinGeckoId, string quoteCurrencyId = "usd-coin");
        Task<decimal?> GetPriceAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin");
        Task<MarketData> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin");
        Task<string?> GetCoinGeckoIdFromSymbol(string symbol);
        Task<IReadOnlyList<CoinList>> GetCoinList();
        Task<IDictionary<string, IDictionary<string, decimal?>>> GetAllPrices(string[] ids, string[]? vsCurrencies = null);
    }
}
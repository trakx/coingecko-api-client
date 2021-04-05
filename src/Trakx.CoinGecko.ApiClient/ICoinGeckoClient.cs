using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoinGecko.Entities.Response.Coins;
using CoinGecko.Entities.Response.Simple;

namespace Trakx.CoinGecko.ApiClient
{
    public interface ICoinGeckoClient
    {
        Task<decimal?> GetLatestPrice(string coinGeckoId, string quoteCurrencyId = "usd-coin");
        Task<decimal?> GetPriceAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin");
        Task<MarketData> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = "usd-coin");
        Task<string?> GetCoinGeckoIdFromSymbol(string symbol);
        Task<IReadOnlyList<CoinList>> GetCoinList();
        Task<Price> GetAllPrices(string[] ids, string[]? vsCurrencies = null);
    }
}
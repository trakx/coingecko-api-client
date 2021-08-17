using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trakx.CoinGecko.ApiClient
{
    public interface ICoinGeckoClient
    {
        Task<decimal?> GetLatestPrice(string coinGeckoId, string quoteCurrencyId = Constants.UsdCoin);
        Task<decimal?> GetPriceAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = Constants.UsdCoin);
        Task<MarketData?> GetMarketDataAsOfFromId(string id, DateTime asOf, string quoteCurrencyId = Constants.UsdCoin);
        Task<string?> GetCoinGeckoIdFromSymbol(string symbol);
        Task<IReadOnlyList<CoinList>> GetCoinList();
        Task<IDictionary<string, IDictionary<string, decimal?>>> GetAllPrices(string[] ids, string[]? vsCurrencies = null);
        Task<IList<ExtendedPrice>> GetAllPricesExtended(string[] ids, string[]? vsCurrencies = null, bool includeMarketCap = false, bool include24HrVol = false);
        Task<IDictionary<DateTimeOffset, MarketData>> GetMarketDataForDateRange(string id, string vsCurrency,
            DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
    }
}
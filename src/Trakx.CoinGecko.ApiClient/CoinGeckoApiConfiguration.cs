using System.Collections.Generic;

namespace Trakx.CoinGecko.ApiClient
{
    public class CoinGeckoApiConfiguration
    {
#nullable disable
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public List<string> FavouriteExchanges { get; set; }
#nullable restore
    }
}
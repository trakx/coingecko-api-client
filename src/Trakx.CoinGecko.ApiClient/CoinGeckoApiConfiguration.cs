namespace Trakx.CoinGecko.ApiClient
{
    public class CoinGeckoApiConfiguration
    {
        
        public string BaseUrl { get; set; }

        public int? MaxRetryCount { get; set; }

        public int? InitialRetryDelayInMilliseconds { get; set; }

        public  int? ThrottleDelayPerSecond { get; set; }

        public int? CacheDurationInSeconds { get; set; }

    }
}
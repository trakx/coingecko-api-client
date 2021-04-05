namespace Trakx.CoinGecko.ApiClient
{
    public record CoinGeckoApiConfiguration
    {
        public int? InitialRetryDelayInMilliseconds { get; init; }

        public int? MaxRetryCount { get; init; }

        public int? ThrottleDelayPerSecond { get; init; }
    }
}
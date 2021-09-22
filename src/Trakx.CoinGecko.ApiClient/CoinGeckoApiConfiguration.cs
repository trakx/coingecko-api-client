using System;
using Trakx.Utils.Attributes;

namespace Trakx.CoinGecko.ApiClient
{
    public record CoinGeckoApiConfiguration
    {
#pragma warning disable CS8618
        public string BaseUrl { get; init; }
#pragma warning restore CS8618

        public int? MaxRetryCount { get; init; }

        public int? InitialRetryDelayInMilliseconds { get; init; }

        public int? ThrottleDelayPerSecond { get; init; }

        public int? CacheDurationInSeconds { get; init; }

        public bool IsPro => BaseUrl.Contains("pro-api", StringComparison.InvariantCultureIgnoreCase);

        [SecretEnvironmentVariable(nameof(CoinGeckoApiConfiguration), nameof(ApiKey))]
        public string ApiKey { get; init; }
    }
}
using System;
using Trakx.Common.Attributes;
using Trakx.Common.Extensions;

namespace Trakx.CoinGecko.ApiClient;

/// <summary>Main configuration for how to connect to the CoinGecko API.</summary>
public record CoinGeckoApiConfiguration
{
    /// <summary>Base Url for all calls to the API. Default is https://pro-api.coingecko.com/api/v3/</summary>
    [AwsParameter(AllowGlobal = true)]
    public Uri BaseUrl { get; init; } = Constants.ProBaseUrl;

    /// <summary>Access key added to all requests to the API.</summary>
    [AwsParameter(AllowGlobal = true)]
    [SecretEnvironmentVariable]
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>How long to cache requests to the API, to reuse them and reduce usage. Default is 10 seconds.</summary>
    [AwsParameter(AllowGlobal = true)]
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>Timeout waiting for a response from the API. Default is 10 seconds.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>How many attempts to retry the call to the API if it's failing. Default is 10 retries.</summary>
    public int MaxRetryCount { get; init; } = 10;

    /// <summary>Median delay to target before first retry. Subsequent retries will have exponentially longer delays. Default is 100 milliseconds.</summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Is the configuration set to the PRO API (or the free API if not).</summary>
    public bool IsPro => BaseUrl.OriginalString.ContainsIgnoreCase("pro-api");
}

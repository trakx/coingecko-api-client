using System;
using Trakx.Common.Attributes;
using Trakx.Common.Extensions;

namespace Trakx.CoinGecko.ApiClient;

public record CoinGeckoApiConfiguration
{
    public Uri BaseUrl { get; init; } = Constants.ProBaseUrl;

    public int MaxRetryCount { get; init; } = 10;

    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    // this setting isn't used anywhere
    public int? ThrottleDelayPerSecond { get; init; } = 120;

    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Timeout waiting for a response from the API. Default is 10 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    public bool IsPro => BaseUrl.OriginalString.ContainsIgnoreCase("pro-api");

    [AwsParameter(AllowGlobal = true)]
    [SecretEnvironmentVariable]
    public string ApiKey { get; init; } = string.Empty;
}

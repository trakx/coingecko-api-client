using System;

namespace Trakx.CoinGecko.ApiClient;

public static class Constants
{
    /// <summary>usd</summary>
    public const string Usd = "usd";

    /// <summary>usd-coin</summary>
    public const string UsdCoin = "usd-coin";

    /// <summary>https://api.coingecko.com/api/v3/</summary>
    public static Uri PublicBaseUrl { get; } = new("https://api.coingecko.com/api/v3/");

    /// <summary>https://pro-api.coingecko.com/api/v3/</summary>
    public static Uri ProBaseUrl { get; } = new("https://pro-api.coingecko.com/api/v3/");
}
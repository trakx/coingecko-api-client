using System.Globalization;

namespace Trakx.CoinGecko.ApiClient;

internal static class Extensions
{
    internal static string ToDateString(this DateTime date)
    {
        return date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    internal static DateTimeOffset AsUnixMillisToDate(this double value)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds((long)value);
    }
}
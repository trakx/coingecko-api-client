using System;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;

namespace Trakx.CoinGecko.ApiClient.Pricing
{
    /// <summary>
    /// Please make sure that the Periods are matched with the number of seconds
    /// they contain.
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumMemberConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
    public enum Period
    {
        [EnumMember(Value = "minute")]
        Minute = 60,
        [EnumMember(Value = "hour")]
        Hour = 60 * 60,
        [EnumMember(Value = "day")]
        Day = 24 * 60 * 60
    }

    public static class PeriodExtensions
    {
        public static TimeSpan ToTimeSpan(this Period? period)
        {
            return period?.ToTimeSpan() ?? TimeSpan.Zero;
        }

        public static TimeSpan ToTimeSpan(this Period period)
        {
            return TimeSpan.FromSeconds((int) period);
        }
    }
}
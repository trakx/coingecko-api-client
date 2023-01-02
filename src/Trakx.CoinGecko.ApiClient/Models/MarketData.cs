using System;

namespace Trakx.CoinGecko.ApiClient;

public record MarketData
{
    public string? CoinId { get; init; }
    public string? Name { get; init; }
    public DateTimeOffset? AsOf { get; init; }
    public string? QuoteCurrency { get; init; }
    public decimal? Price { get; init; }
    public decimal? Volume { get; init; }
    public decimal? MarketCap { get; init; }
    public string? CoinSymbol { get; init; }
}
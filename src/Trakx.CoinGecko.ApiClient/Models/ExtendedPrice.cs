namespace Trakx.CoinGecko.ApiClient;

public class ExtendedPrice
{
    public string? CoinGeckoId { get; init; }
    public string? Currency { get; init; }
    public decimal Price { get; init; }
    public decimal? MarketCap { get; init; }
    public decimal? DailyVolume { get; init; }
}
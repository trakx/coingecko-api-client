namespace Trakx.CoinGecko.ApiClient.Models;

public record PricesForSymbols
{
    public required SymbolToCoinGeckoIdsMap SymbolToIdMap { get; set; }
    public required MultiplePrices Prices { get; set; }
}
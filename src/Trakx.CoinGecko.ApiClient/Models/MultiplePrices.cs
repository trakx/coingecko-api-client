using System.Collections.Generic;
using System.Linq;

namespace Trakx.CoinGecko.ApiClient.Models;

public class MultiplePrices
{
    private readonly IDictionary<string, IDictionary<string, decimal?>> _source;
    private readonly string[] _quoteCurrencies;

    public MultiplePrices(IDictionary<string, IDictionary<string, decimal?>> source)
    {
        _source = source;
        _quoteCurrencies = _source.Values.SelectMany(p => p.Keys).Distinct().ToArray();
    }

    public int TotalPriceCount => _source.Sum(p => p.Value.Count);

    public decimal GetPrice(string coinGeckoId, string quoteCurrencyId = Constants.Usd)
    {
        var directPrice = TryGetPrice(coinGeckoId, quoteCurrencyId);
        if (directPrice != default) return directPrice;

        foreach (var supportedQuote in _quoteCurrencies)
        {
            var conversionRate = TryGetPrice(quoteCurrencyId, supportedQuote);
            if (conversionRate == default) continue;

            var supportedPrice = TryGetPrice(coinGeckoId, supportedQuote);
            if (supportedPrice == default) continue;

            var convertedPrice = supportedPrice / conversionRate;
            return convertedPrice;
        }

        return default;
    }

    private decimal TryGetPrice(string coinGeckoId, string quoteCurrencyId)
    {
        _source.TryGetValue(coinGeckoId, out var prices);
        if (prices == null) return default;

        prices.TryGetValue(quoteCurrencyId, out var price);
        return price.GetValueOrDefault();
    }
}

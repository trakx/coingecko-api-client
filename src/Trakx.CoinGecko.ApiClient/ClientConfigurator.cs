using System.Net.Http;

namespace Trakx.CoinGecko.ApiClient;

internal class ClientConfigurator
{
    private const string proHeader = "X-Cg-Pro-Api-Key";
        
    public ClientConfigurator(CoinGeckoApiConfiguration configuration)
    {
        ApiConfiguration = configuration;
    }

    public CoinGeckoApiConfiguration ApiConfiguration { get; }

    public void AddHeaders(HttpClient client)
    {
        if (ApiConfiguration.IsPro)
            client.DefaultRequestHeaders.Add(proHeader, ApiConfiguration.ApiKey);
    }
}
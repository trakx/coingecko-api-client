using System.Net.Http;

namespace Trakx.CoinGecko.ApiClient;

public class ClientConfigurator
{
    private const string ProHeader = "X-Cg-Pro-Api-Key";

    public CoinGeckoApiConfiguration Configuration { get; }

    public ClientConfigurator(CoinGeckoApiConfiguration configuration)
    {
        Configuration = configuration;
    }

    internal void ApplyConfiguration(HttpClient client)
    {
        if (Configuration.IsPro && !client.DefaultRequestHeaders.Contains(ProHeader))
            client.DefaultRequestHeaders.Add(ProHeader, Configuration.ApiKey);

        if (Configuration.Timeout != default)
            client.Timeout = Configuration.Timeout;
    }
}
using System.Net.Http;

namespace Trakx.CoinGecko.ApiClient;

internal class ClientConfigurator
{
    private const string ProHeader = "X-Cg-Pro-Api-Key";

    internal CoinGeckoApiConfiguration Configuration { get; }

    internal ClientConfigurator(CoinGeckoApiConfiguration configuration)
    {
        Configuration = configuration;
    }

    internal void ApplyConfiguration(HttpClient client)
    {
        if (Configuration.IsPro)
            client.DefaultRequestHeaders.Add(ProHeader, Configuration.ApiKey);

        if (Configuration.Timeout != default)
            client.Timeout = Configuration.Timeout;
    }
}
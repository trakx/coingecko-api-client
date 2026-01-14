namespace Trakx.CoinGecko.ApiClient;

internal class ClientConfigurator(CoinGeckoApiConfiguration configuration)
{
    private const string ProHeader = "X-Cg-Pro-Api-Key";

    internal CoinGeckoApiConfiguration Configuration { get; } = configuration;

    internal void ApplyConfiguration(HttpClient client)
    {
        var baseUrl = Configuration.BaseUrl.AbsoluteUri.TrimEnd('/') + "/";
        client.BaseAddress = new Uri(baseUrl);

        if (Configuration.IsPro && !client.DefaultRequestHeaders.Contains(ProHeader))
            client.DefaultRequestHeaders.Add(ProHeader, Configuration.ApiKey);

        if (Configuration.Timeout != TimeSpan.Zero)
            client.Timeout = Configuration.Timeout;
    }
}
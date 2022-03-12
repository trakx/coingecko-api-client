namespace Trakx.CoinGecko.ApiClient;

internal abstract class AuthorisedClient
{
    public readonly CoinGeckoApiConfiguration Configuration;
    protected string BaseUrl => Configuration!.BaseUrl;

    protected AuthorisedClient(ClientConfigurator clientConfigurator)
    {
        Configuration = clientConfigurator.ApiConfiguration;
    }
}
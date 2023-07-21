namespace Trakx.CoinGecko.ApiClient;

internal abstract class AuthorisedClient
{
    internal string BaseUrl { get; }

    protected AuthorisedClient(ClientConfigurator configurator)
    {
        BaseUrl = configurator.Configuration.BaseUrl.OriginalString;
    }
}
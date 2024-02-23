namespace Trakx.CoinGecko.ApiClient;

public abstract class AuthorisedClient
{
    public string BaseUrl { get; }

    protected AuthorisedClient(ClientConfigurator configurator)
    {
        BaseUrl = configurator.Configuration.BaseUrl.AbsoluteUri;
        if (BaseUrl[^1] != '/') BaseUrl += "/";
    }
}
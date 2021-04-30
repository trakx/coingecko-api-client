namespace Trakx.CoinGecko.ApiClient
{
    internal class ClientConfigurator
    {

        public ClientConfigurator(CoinGeckoApiConfiguration configuration)
        {
            ApiConfiguration = configuration;
        }

        public CoinGeckoApiConfiguration ApiConfiguration { get; }

    }
}
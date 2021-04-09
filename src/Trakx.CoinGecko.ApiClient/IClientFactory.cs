namespace Trakx.CoinGecko.ApiClient
{
    public  interface IClientFactory
    {

        ICoinsClient CreateCoinsClient();

        ISimpleClient CreateSimpleClient();

    }
}

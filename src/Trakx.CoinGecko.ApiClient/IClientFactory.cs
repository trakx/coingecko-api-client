namespace Trakx.CoinGecko.ApiClient
{
    internal interface IClientFactory
    {
        ICoinsClient CreateCoinsClient();
        ISimpleClient CreateSimpleClient();
    }
}

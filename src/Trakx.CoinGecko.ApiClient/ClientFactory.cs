using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Trakx.CoinGecko.ApiClient
{
    public class ClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public global::CoinGecko.Interfaces.ICoinsClient CreateCoinsClient()
        {
            return _serviceProvider.GetRequiredService<global::CoinGecko.Interfaces.ICoinsClient>();
        }

        public global::CoinGecko.Interfaces.ISimpleClient CreateSimpleClient()
        {
            return _serviceProvider.GetRequiredService<global::CoinGecko.Interfaces.ISimpleClient>();
        }
    }
}

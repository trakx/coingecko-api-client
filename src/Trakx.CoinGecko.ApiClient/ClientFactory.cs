using System;
using Microsoft.Extensions.DependencyInjection;

namespace Trakx.CoinGecko.ApiClient
{
    public class ClientFactory : IClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ClientFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICoinsClient CreateCoinsClient()
        {
            return _serviceProvider.GetRequiredService<ICoinsClient>();
        }

        public ISimpleClient CreateSimpleClient()
        {
            return _serviceProvider.GetRequiredService<ISimpleClient>();
        }

    }
}

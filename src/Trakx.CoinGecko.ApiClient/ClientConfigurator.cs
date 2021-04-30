using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Trakx.CoinGecko.ApiClient
{
    internal class ClientConfigurator
    {

        private readonly IServiceProvider _serviceProvider;

        public ClientConfigurator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            ApiConfiguration = serviceProvider.GetService<IOptions<CoinGeckoApiConfiguration>>()!.Value;
        }

        public CoinGeckoApiConfiguration ApiConfiguration { get; }

    }
}
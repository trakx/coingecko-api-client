using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Trakx.Utils.Apis;

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

        public ICredentialsProvider GetCredentialProvider(Type clientType)
        {
            switch (clientType.Name)
            {
                default:
                    return new NoCredentialsProvider();
            }
        }
    }
}
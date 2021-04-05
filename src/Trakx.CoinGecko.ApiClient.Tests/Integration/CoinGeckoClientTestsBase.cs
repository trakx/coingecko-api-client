using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    [Collection(nameof(ApiTestCollection))]
    public class CoinGeckoClientTestsBase
    {
        protected ServiceProvider ServiceProvider;
        protected ILogger Logger;

        public CoinGeckoClientTestsBase(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
        {
            Logger = new LoggerConfiguration().WriteTo.TestOutput(output).CreateLogger();

            ServiceProvider = apiFixture.ServiceProvider;
        }
    }

    [CollectionDefinition(nameof(ApiTestCollection))]
    public class ApiTestCollection : ICollectionFixture<CoinGeckoApiFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class CoinGeckoApiFixture : IDisposable
    {
        public ServiceProvider ServiceProvider { get; }

        public CoinGeckoApiFixture()
        {
            var secrets = new Secrets();
            var configuration = new CoinGeckoApiConfiguration
            {
                ApiKey = secrets.CoinGeckoApiKey,
                ApiSecret = secrets.CoinGeckoApiSecret,
                BaseUrl = "https://api.coingecko.com/api/v3"
            };

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(configuration);
            serviceCollection.AddCoinGeckoClient(configuration);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            ServiceProvider.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
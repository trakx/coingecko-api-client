using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trakx.Common.Infrastructure.Environment.Aws;
using Trakx.Common.Infrastructure.Environment.Resolvers;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

[CollectionDefinition(nameof(ApiTestCollection), DisableParallelization = true)]
public class ApiTestCollection : ICollectionFixture<CoinGeckoApiFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class CoinGeckoApiFixture : IDisposable
{
    internal static readonly Uri FreeBaseUrl = new("https://api.coingecko.com/api/v3");
    internal static readonly Uri ProBaseUrl = new("https://pro-api.coingecko.com/api/v3");

    public ServiceProvider ServiceProvider { get; }
    public CoinGeckoApiConfiguration Configuration { get; }

    public CoinGeckoApiFixture()
    {
        Configuration = BuildConfiguration();
        ServiceProvider = BuildServiceProvider();
    }

    private static CoinGeckoApiConfiguration BuildConfiguration()
    {
        const string environment = "CiCd";
        var configBuilder = new ConfigurationBuilder().AddAwsSystemManagerConfiguration(environment,
           assemblyResolver: new GenericSecretsAssemblyResolver<CoinGeckoApiConfiguration>());
        IConfiguration configurationRoot = configBuilder.Build();

        return
            configurationRoot.GetSection(nameof(CoinGeckoApiConfiguration)).Get<CoinGeckoApiConfiguration>()!
                with
            {
                BaseUrl = ProBaseUrl,
                MaxRetryCount = 5,
                CacheDuration = TimeSpan.FromSeconds(20),
            };
    }

    private ServiceProvider BuildServiceProvider()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(Configuration);
        serviceCollection.AddCoinGeckoClient(Configuration);
        return serviceCollection.BuildServiceProvider();
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

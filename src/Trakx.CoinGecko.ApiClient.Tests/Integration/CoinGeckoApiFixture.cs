using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trakx.Common.Configuration;
using Trakx.Common.Infrastructure.Caching;
using Trakx.Common.Infrastructure.Environment.Aws;
using Trakx.Common.Infrastructure.Environment.Env;
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
    public ServiceProvider ServiceProvider { get; }
    public CoinGeckoApiConfiguration Configuration { get; }

    public CoinGeckoApiFixture()
    {
        var configurationRoot = BuildConfiguration();

        Configuration = configurationRoot.GetConfiguration<CoinGeckoApiConfiguration>()
            with
        {
            //MaxRetryCount = 5,
            //CacheDuration = TimeSpan.FromSeconds(20),
        };

        var cacheConfiguration = configurationRoot.GetConfiguration<RedisCacheConfiguration>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddCoinGeckoClient(Configuration, cacheConfiguration);

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var environment = EnvironmentProvider.DeploymentEnvironment;
        var assemblyResolver = new GenericSecretsAssemblyResolver<CoinGeckoApiConfiguration>();

        return
            new ConfigurationBuilder()
            .AddAwsSystemManagerConfiguration(environment, assemblyResolver)
            .Build();
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

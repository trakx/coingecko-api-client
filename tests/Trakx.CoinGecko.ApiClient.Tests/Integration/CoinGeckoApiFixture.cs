using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    private ServiceProvider? _serviceProvider;
    public ServiceProvider ServiceProvider => _serviceProvider ??= ServiceCollection.BuildServiceProvider();

    public ServiceCollection ServiceCollection { get; }

    public CoinGeckoApiFixture()
    {
        var configurationRoot = BuildConfiguration();

        var apiConfiguration = configurationRoot.GetConfiguration<CoinGeckoApiConfiguration>();
        var cacheConfiguration = configurationRoot.GetConfiguration<RedisCacheConfiguration>();

        ServiceCollection = new ServiceCollection();
        ServiceCollection.AddCoinGeckoClient(apiConfiguration, cacheConfiguration);
        SetupMemoryDistributedCache(ServiceCollection);
    }

    private static void SetupMemoryDistributedCache(ServiceCollection serviceCollection)
    {
        ServiceDescriptor descriptor = new(
            typeof(IDistributedCache),
            typeof(MemoryDistributedCache),
            ServiceLifetime.Singleton);

        serviceCollection.Replace(descriptor);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var environment = EnvironmentProvider.DeploymentEnvironment;
        var assemblyResolver = new GenericSecretsAssemblyResolver<CoinGeckoApiConfiguration>();

        return
            new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
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

﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Trakx.Utils.Testing;
using Xunit;
using Xunit.Abstractions;

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
    public const string CoinGeckoBaseUrl = "https://api.coingecko.com/api/v3";
    public const string CoinGeckoProBaseUrl = "https://pro-api.coingecko.com/api/v3";

    public ITestOutputHelper Output { get; }
    public ServiceProvider ServiceProvider { get; }
    public CoinGeckoApiConfiguration Configuration { get; }

    public CoinGeckoApiFixture(ITestOutputHelper output)
    {
        Output = output;
        Configuration = BuildConfiguration();
        ServiceProvider = BuildServiceProvider();
    }

    private static CoinGeckoApiConfiguration BuildConfiguration()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("OPTIONAL_AWS_CONFIGURATION", "true");

        return ConfigurationHelper.GetConfigurationFromAws<CoinGeckoApiConfiguration>()
            with
        {
            BaseUrl = CoinGeckoProBaseUrl,
            MaxRetryCount = 5,
            ThrottleDelayPerSecond = 120,
            CacheDurationInSeconds = 20,
            InitialRetryDelayInMilliseconds = 100
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
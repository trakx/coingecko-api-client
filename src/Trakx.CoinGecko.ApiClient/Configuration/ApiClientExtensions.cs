using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Trakx.Common.ApiClient;
using Trakx.Common.Configuration;
using Trakx.Common.DateAndTime;
using Trakx.Common.Infrastructure.Caching;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public static partial class ApiClientExtensions
{
    public static IServiceCollection AddCoinGeckoClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        var apiConfiguration = configuration.GetConfiguration<CoinGeckoApiConfiguration>();
        var cacheConfiguration = configuration.GetConfiguration<RedisCacheConfiguration>();
        return services.AddCoinGeckoClient(apiConfiguration, cacheConfiguration);
    }

    public static IServiceCollection AddCoinGeckoClient(
        this IServiceCollection services,
        CoinGeckoApiConfiguration apiConfiguration,
        RedisCacheConfiguration cacheConfiguration)
    {
        // api configuration
        services.AddSingleton(apiConfiguration);
        services.AddSingleton(new ClientConfigurator(apiConfiguration));

        // required common services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddDistributedCache(cacheConfiguration);
        services.AddMemoryCache();

        // clients: api and http
        services.AddSingleton<ICoinGeckoClient, CoinGeckoClient>();
        services.AddTransient<CachedHttpClientHandler>();
        services.AddHttpClientsForCoinGeckoClients(apiConfiguration);

        return services;
    }

    private static IServiceCollection AddHttpClientsForCoinGeckoClients(
        this IServiceCollection services, CoinGeckoApiConfiguration configuration)
    {
        var delays = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: configuration.InitialRetryDelay,
            retryCount: configuration.MaxRetryCount,
            fastFirst: false)
            .ToList();

        var configurator = new ClientConfigurator(configuration);

        return services
            .AddHttpClientForCoinGeckoClient<ICoinsClient, CoinsClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IContractClient, ContractClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IDerivativesClient, DerivativesClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IEventsClient, EventsClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IExchange_ratesClient, Exchange_ratesClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IExchangesClient, ExchangesClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IFinanceClient, FinanceClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IGlobalClient, GlobalClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IIndexesClient, IndexesClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IPingClient, PingClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<ISimpleClient, SimpleClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<IStatus_updatesClient, Status_updatesClient>(configurator, delays)
            .AddHttpClientForCoinGeckoClient<ITrendingClient, TrendingClient>(configurator, delays);
    }

    internal static IServiceCollection AddHttpClientForCoinGeckoClient<TInterface, TImplementation>(
        this IServiceCollection services, ClientConfigurator configurator, List<TimeSpan> delays)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        var maxRetryCount = configurator.Configuration.MaxRetryCount;

        var clientType = typeof(TImplementation);

        services
            .AddHttpClient<TInterface, TImplementation>(clientType.FullName!, configurator.ApplyConfiguration)
            .AddHttpMessageHandler<CachedHttpClientHandler>()
            .AddPolicyHandler((serviceProvider, _) =>
            {
                var dateTimeProvider = serviceProvider.GetRequiredService<IDateTimeProvider>();

                return Policy<HttpResponseMessage>
                .Handle<ApiException>()
                .OrTransientHttpStatusCode()
                .WaitAndRetryAsync(
                    retryCount: maxRetryCount,

                    sleepDurationProvider: (retryCount, response, _) =>
                        GetServerWaitDuration(dateTimeProvider, response, delays[retryCount - 1]),

                    onRetryAsync: async (result, timeSpan, retryCount, context) =>
                    {
                        ILogger logger = LoggerProvider.Create(clientType);
                        await logger.LogApiFailureAsync(result, timeSpan, retryCount, context);
                    })
                .WithPolicyKey(clientType.FullName);
            });

        return services;
    }

    private static TimeSpan GetServerWaitDuration(
        IDateTimeProvider dateTimeProvider,
        DelegateResult<HttpResponseMessage> response, TimeSpan minDelay)
    {
        var retryAfter = response.Result?.Headers?.RetryAfter;
        if (retryAfter == null) return default;

        var waitDuration = retryAfter.Delta.GetValueOrDefault();

        if (retryAfter.Date.HasValue)
        {
            var now = dateTimeProvider.UtcNow;
            waitDuration = retryAfter.Date.Value - now;
        }

        return new[] { waitDuration, minDelay }.Max();
    }
}
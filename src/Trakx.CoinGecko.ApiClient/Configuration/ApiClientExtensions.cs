using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Trakx.Common.ApiClient;
using Trakx.Common.Logging;

namespace Trakx.CoinGecko.ApiClient;

public static partial class ApiClientExtensions
{
    public static IServiceCollection AddCoinGeckoClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(nameof(CoinGeckoApiConfiguration));
        services.Configure<CoinGeckoApiConfiguration>(section);

        var typedConfig = section.Get<CoinGeckoApiConfiguration>()!;
        return services.AddCoinGeckoClient(typedConfig);
    }

    public static IServiceCollection AddCoinGeckoClient(
        this IServiceCollection services, CoinGeckoApiConfiguration apiConfiguration)
    {
        services.AddSingleton(apiConfiguration);
        services.AddSingleton(services => new ClientConfigurator(apiConfiguration));

        services.AddSingleton<ICoinGeckoClient, CoinGeckoClient>();
        services.AddTransient<CachedHttpClientHandler>();

        services.AddSingleton<ISemaphore>(new Semaphore(new SemaphoreSlim(1, 1)));
        services.AddMemoryCache();

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
            .AddPolicyHandler((s, request) =>
                Policy<HttpResponseMessage>
                .Handle<ApiException>()
                .OrTransientHttpStatusCode()
                .WaitAndRetryAsync(
                    retryCount: maxRetryCount,

                    sleepDurationProvider: (retryCount, response, context) =>
                        GetServerWaitDuration(response, delays[retryCount - 1]),

                    onRetryAsync: async (result, timeSpan, retryCount, context) =>
                    {
                        ILogger logger = LoggerProvider.Create(clientType);
                        await logger.LogApiFailureAsync(result, timeSpan, retryCount, context);
                    })

                .WithPolicyKey(clientType.FullName));

        return services;
    }

    private static TimeSpan GetServerWaitDuration(DelegateResult<HttpResponseMessage> response, TimeSpan minDelay)
    {
        var retryAfter = response.Result?.Headers?.RetryAfter;
        if (retryAfter == null) return TimeSpan.Zero;

        var waitDuration = retryAfter.Date.HasValue
            ? retryAfter.Date.Value - DateTime.UtcNow
            : retryAfter.Delta.GetValueOrDefault(TimeSpan.Zero);

        return new[] { waitDuration, minDelay }.Max();
    }
}
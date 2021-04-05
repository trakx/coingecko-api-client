using System;
using System.Net.Http;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Serilog;

namespace Trakx.CoinGecko.ApiClient
{
    public static partial class AddCoinGeckoApiClientExtension
    {
        public static IServiceCollection AddCoinGeckoClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            var apiConfig = configuration.GetSection(nameof(CoinGeckoApiConfiguration))
                .Get<CoinGeckoApiConfiguration>()!;
            services.Configure<CoinGeckoApiConfiguration>(configuration.GetSection(nameof(CoinGeckoApiConfiguration)));

            AddCommonDependencies(services, apiConfig);
            return services;
        }

        public static IServiceCollection AddCoinGeckoClient(this IServiceCollection services, CoinGeckoApiConfiguration configuration)
        {
            var options = Options.Create(configuration);
            services.AddSingleton(options);

            AddCommonDependencies(services, configuration);
            return services;
        }

        private static void AddCommonDependencies(IServiceCollection services, CoinGeckoApiConfiguration configuration)
        {
            services.AddSingleton<ClientFactory>();
            services.AddSingleton<ICoinGeckoClient, CoinGeckoClient>();
            services.AddSingleton<ISimpleClient, SimpleClient>();
            services.AddSingleton<ICoinsClient, CoinsClient>();
            services.AddClients(configuration);
        }

        private static void LogFailure(ILogger logger, DelegateResult<HttpResponseMessage> result, TimeSpan timeSpan, int retryCount, Context context)
        {
            if (result.Exception != null)
            {
                logger.Warning(result.Exception, "An exception occurred on retry {RetryAttempt} for {PolicyKey}. Retrying in {SleepDuration}ms.",
                    retryCount, context.PolicyKey, timeSpan.TotalMilliseconds);
            }
            else
            {
                logger.Warning("A non success code {StatusCode} with reason {Reason} and content {Content} was received on retry {RetryAttempt} for {PolicyKey}. Retrying in {SleepDuration}ms.",
                    (int)result.Result.StatusCode, result.Result.ReasonPhrase,
                    result.Result.Content?.ReadAsStringAsync().GetAwaiter().GetResult(),
                    retryCount, context.PolicyKey, timeSpan.TotalMilliseconds);
            }
        }
    }
}
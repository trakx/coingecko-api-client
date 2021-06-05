using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog;
using Trakx.Utils.DateTimeHelpers;

namespace Trakx.CoinGecko.ApiClient
{
    public static partial class AddCoinGeckoClientExtension
    {
        public static IServiceCollection AddCoinGeckoClient(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<CoinGeckoApiConfiguration>(configuration.GetSection(nameof(CoinGeckoApiConfiguration)));
            var typedConfig = configuration.GetSection(nameof(CoinGeckoApiConfiguration))
                .Get<CoinGeckoApiConfiguration>();
            AddCoinGeckoClient(services, typedConfig);

            return services;
        }

        public static IServiceCollection AddCoinGeckoClient(
            this IServiceCollection services, CoinGeckoApiConfiguration apiConfiguration)
        {
            services.AddSingleton(apiConfiguration);
            services.AddSingleton<ICoinGeckoClient, CoinGeckoClient>();
            services.AddSingleton<ISemaphore>(new Semaphore(new SemaphoreSlim(1, 1)));
            services.AddMemoryCache();
            services.AddClients(apiConfiguration);
            services.AddTransient<CachedHttpClientHandler>();

            AddCommonDependencies(services);

            return services;
        }

        private static void AddCommonDependencies(IServiceCollection services)
        {
            services.AddSingleton<ClientConfigurator>();
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
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
                    result.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                    retryCount, context.PolicyKey, timeSpan.TotalMilliseconds);
            }
        }
    }
}

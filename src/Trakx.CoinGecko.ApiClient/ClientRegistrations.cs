using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Serilog;

namespace Trakx.CoinGecko.ApiClient
{
    public static partial class AddCoinGeckoClientExtension
    {
        private static void AddClients(this IServiceCollection services)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(100), retryCount: 10, fastFirst: true);

            services.AddHttpClient<ICoinsClient, CoinsClient>("Trakx.Shrimpy.ApiClient.CoinsClient")
                .AddPolicyHandler((s, request) =>
                    Policy<HttpResponseMessage>
                        .Handle<ApiException>()
                        .Or<HttpRequestException>()
                        .OrTransientHttpStatusCode()
                        .WaitAndRetryAsync(delay,
                            onRetry: (result, timeSpan, retryCount, context) =>
                            {
                                var logger = Log.Logger.ForContext<CoinsClient>();
                                LogFailure(logger, result, timeSpan, retryCount, context);
                            })
                        .WithPolicyKey("Trakx.CoinGecko.ApiClient.CoinsClient"));

            services.AddHttpClient<ISimpleClient, SimpleClient>("Trakx.Shrimpy.ApiClient.SimpleClient")
                .AddPolicyHandler((s, request) =>
                    Policy<HttpResponseMessage>
                        .Handle<ApiException>()
                        .Or<HttpRequestException>()
                        .OrTransientHttpStatusCode()
                        .WaitAndRetryAsync(delay,
                            onRetry: (result, timeSpan, retryCount, context) =>
                            {
                                var logger = Log.Logger.ForContext<SimpleClient>();
                                LogFailure(logger, result, timeSpan, retryCount, context);
                            })
                        .WithPolicyKey("Trakx.CoinGecko.ApiClient.SimpleClient"));

        }
    }
}

using System;
using System.Net.Http;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Serilog;


namespace Trakx.CoinGecko.ApiClient
{
    public static partial class AddCoinGeckoApiClientExtension
    {
        private static void AddClients(this IServiceCollection services, CoinGeckoApiConfiguration configuration)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromMilliseconds(configuration.InitialRetryDelayInMilliseconds ?? 100), 
                retryCount: configuration.MaxRetryCount ?? 10, fastFirst: true);

            var handler = new ThrottledHttpClientHandler(configuration.ThrottleDelayPerSecond ?? 100);
                                    
            services.AddHttpClient<ICoinsClient, CoinsClient>("Trakx.CoinGecko.ApiClient.CoinsClient")
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    .Or<HttpRequestException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(delay,
                        onRetry: (result, timeSpan, retryCount, context) =>
                        {
                            var logger = Log.Logger.ForContext<CoinsClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.CoinsClient"));

                                
            services.AddHttpClient<ISimpleClient, SimpleClient>("Trakx.CoinGecko.ApiClient.SimpleClient")
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.Forbidden)
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
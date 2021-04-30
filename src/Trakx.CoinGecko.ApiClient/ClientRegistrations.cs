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
        private static void AddClients(this IServiceCollection services, CoinGeckoApiConfiguration configuration)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromMilliseconds(configuration.InitialRetryDelayInMilliseconds ?? 100), 
                retryCount: configuration.MaxRetryCount ?? 10, fastFirst: true);

                                    
            services.AddHttpClient<IPingClient, PingClient>("Trakx.CoinGecko.ApiClient.PingClient")
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
                            var logger = Log.Logger.ForContext<PingClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.PingClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<ISimpleClient, SimpleClient>("Trakx.CoinGecko.ApiClient.SimpleClient")
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
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.SimpleClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<ICoinsClient, CoinsClient>("Trakx.CoinGecko.ApiClient.CoinsClient")
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
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.CoinsClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IContractClient, ContractClient>("Trakx.CoinGecko.ApiClient.ContractClient")
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
                            var logger = Log.Logger.ForContext<ContractClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.ContractClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IExchangesClient, ExchangesClient>("Trakx.CoinGecko.ApiClient.ExchangesClient")
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
                            var logger = Log.Logger.ForContext<ExchangesClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.ExchangesClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IFinanceClient, FinanceClient>("Trakx.CoinGecko.ApiClient.FinanceClient")
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
                            var logger = Log.Logger.ForContext<FinanceClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.FinanceClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IIndexesClient, IndexesClient>("Trakx.CoinGecko.ApiClient.IndexesClient")
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
                            var logger = Log.Logger.ForContext<IndexesClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.IndexesClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IDerivativesClient, DerivativesClient>("Trakx.CoinGecko.ApiClient.DerivativesClient")
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
                            var logger = Log.Logger.ForContext<DerivativesClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.DerivativesClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IStatus_updatesClient, Status_updatesClient>("Trakx.CoinGecko.ApiClient.Status_updatesClient")
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
                            var logger = Log.Logger.ForContext<Status_updatesClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.Status_updatesClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IEventsClient, EventsClient>("Trakx.CoinGecko.ApiClient.EventsClient")
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
                            var logger = Log.Logger.ForContext<EventsClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.EventsClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IExchange_ratesClient, Exchange_ratesClient>("Trakx.CoinGecko.ApiClient.Exchange_ratesClient")
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
                            var logger = Log.Logger.ForContext<Exchange_ratesClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.Exchange_ratesClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<ITrendingClient, TrendingClient>("Trakx.CoinGecko.ApiClient.TrendingClient")
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
                            var logger = Log.Logger.ForContext<TrendingClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.TrendingClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IGlobalClient, GlobalClient>("Trakx.CoinGecko.ApiClient.GlobalClient")
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
                            var logger = Log.Logger.ForContext<GlobalClient>();
                            LogFailure(logger, result, timeSpan, retryCount, context);
                        })
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.GlobalClient"))
                .AddHttpMessageHandler<CachedHttpClientHandler>();

        }
    }
}

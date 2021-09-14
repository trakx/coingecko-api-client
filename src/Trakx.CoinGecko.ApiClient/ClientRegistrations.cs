﻿using System;
using System.Linq;
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
	    private const string proKey = "X-Cg-Pro-Api-Key";
	    
        private static void AddClients(this IServiceCollection services, CoinGeckoApiConfiguration configuration)
        {
            var maxRetryCount = configuration.MaxRetryCount ?? 10;
            var delays = Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromMilliseconds(configuration.InitialRetryDelayInMilliseconds ?? 100), 
                retryCount: maxRetryCount, fastFirst: false).ToList();

            services.AddHttpClient<IPingClient, PingClient>("Trakx.CoinGecko.ApiClient.PingClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<PingClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.PingClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<ISimpleClient, SimpleClient>("Trakx.CoinGecko.ApiClient.SimpleClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<SimpleClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.SimpleClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<ICoinsClient, CoinsClient>("Trakx.CoinGecko.ApiClient.CoinsClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<CoinsClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.CoinsClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IContractClient, ContractClient>("Trakx.CoinGecko.ApiClient.ContractClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<ContractClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.ContractClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IExchangesClient, ExchangesClient>("Trakx.CoinGecko.ApiClient.ExchangesClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<ExchangesClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.ExchangesClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IFinanceClient, FinanceClient>("Trakx.CoinGecko.ApiClient.FinanceClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<FinanceClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.FinanceClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IIndexesClient, IndexesClient>("Trakx.CoinGecko.ApiClient.IndexesClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<IndexesClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.IndexesClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IDerivativesClient, DerivativesClient>("Trakx.CoinGecko.ApiClient.DerivativesClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<DerivativesClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.DerivativesClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IStatus_updatesClient, Status_updatesClient>("Trakx.CoinGecko.ApiClient.Status_updatesClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<Status_updatesClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.Status_updatesClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IEventsClient, EventsClient>("Trakx.CoinGecko.ApiClient.EventsClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<EventsClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.EventsClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IExchange_ratesClient, Exchange_ratesClient>("Trakx.CoinGecko.ApiClient.Exchange_ratesClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<Exchange_ratesClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.Exchange_ratesClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<ITrendingClient, TrendingClient>("Trakx.CoinGecko.ApiClient.TrendingClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<TrendingClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.TrendingClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

                                
            services.AddHttpClient<IGlobalClient, GlobalClient>("Trakx.CoinGecko.ApiClient.GlobalClient", ConfigureClient)
                .AddPolicyHandler((s, request) => 
                    Policy<HttpResponseMessage>
                    .Handle<ApiException>()
                    .OrTransientHttpStatusCode()
                    .WaitAndRetryAsync(
		                retryCount: maxRetryCount,
		                sleepDurationProvider: (retryCount, response, context) => 
			                GetServerWaitDuration(response, delays[retryCount - 1]),
		                onRetryAsync: (result, timeSpan, retryCount, context) =>
							LogFailure(Log.Logger.ForContext<GlobalClient>(), result, timeSpan, retryCount, context))
                    .WithPolicyKey("Trakx.CoinGecko.ApiClient.GlobalClient"))
                    
                .AddHttpMessageHandler<CachedHttpClientHandler>();

        }

        private static void ConfigureClient(HttpClient client)
        {
	        client.DefaultRequestHeaders.Add(proKey, Environment.GetEnvironmentVariable("COINGECKO_API_KEY"));
        }
    }
}

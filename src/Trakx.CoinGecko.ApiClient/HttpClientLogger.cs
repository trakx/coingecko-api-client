using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

namespace Trakx.CoinGecko.ApiClient;

public class HttpClientLogger : IHttpClientLogger
{
    private readonly ILogger<HttpClientLogger> _logger;

    public HttpClientLogger(ILogger<HttpClientLogger> logger)
    {
        _logger = logger;
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        _logger.LogInformation(
            "{Method} {OriginalString}",
            request.Method,
            request.RequestUri!.OriginalString);

        return null;
    }

    public void LogRequestStop(
        object? context,
        HttpRequestMessage request,
        HttpResponseMessage response,
        TimeSpan elapsed)
    {
        // don't log 200 OK
        if (response.StatusCode == System.Net.HttpStatusCode.OK) return;

        _logger.LogInformation(
            "Received '{StatusCodeInt} {StatusCodeString}' after {TotalMilliseconds}ms",
            (int)response.StatusCode,
            response.StatusCode,
            elapsed.TotalMilliseconds);
    }

    public void LogRequestFailed(
        object? context,
        HttpRequestMessage request,
        HttpResponseMessage? response,
        Exception exception,
        TimeSpan elapsed)
    {
        _logger.LogError(
            exception,
            "Request '{OriginalString}' failed after {TotalMilliseconds}ms",
            request.RequestUri!.OriginalString,
            elapsed.TotalMilliseconds);
    }
}
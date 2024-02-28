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
            "{Request.Method} {Request.Host}{Request.Path}",
            request.Method,
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri!.PathAndQuery);

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
            "Received '{Response.StatusCodeInt} {Response.StatusCodeString}' after {Response.ElapsedMilliseconds}ms",
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
            "Request '{Request.Host}{Request.Path}' failed after {Response.ElapsedMilliseconds}ms",
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri!.PathAndQuery,
            elapsed.TotalMilliseconds);
    }
}
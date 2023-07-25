using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Trakx.CoinGecko.ApiClient.Tests.Unit;

public class CachedHttpClientHandlerTests
{
    private static readonly CancellationToken Cancellation = CancellationToken.None;

    private readonly IDistributedCache _cache;
    private readonly TestMessageHandler _innerHandler;
    private readonly CachedHttpClientHandler _handler;

    public CachedHttpClientHandlerTests()
    {
        var configuration = new CoinGeckoApiConfiguration();

        _cache = Substitute.For<IDistributedCache>();

        _innerHandler = new TestMessageHandler();

        _handler = new CachedHttpClientHandler(_cache, configuration)
        {
            InnerHandler = _innerHandler
        };
    }

    [Fact]
    public async Task SendAsyncInternal_skips_cache_if_method_is_not_get()
    {
        _innerHandler.SetupResponse(HttpStatusCode.NotFound, string.Empty);

        HttpRequestMessage request = CreateRequest(HttpMethod.Post);

        _ = await _handler.SendAsyncInternal(request, Cancellation);

        _cache.ReceivedCalls().Should().BeEmpty();
        _innerHandler.CallsToSendAsync.Should().Be(1);
    }

    [Fact]
    public async Task SendAsyncInternal_returns_cached_response_if_available()
    {
        HttpRequestMessage request = CreateRequest(HttpMethod.Get);
        CachedHttpResponse cachedResponse = await SetupCachedResponse(request);

        var response = await _handler.SendAsyncInternal(request, Cancellation);

        response.StatusCode.Should().Be(cachedResponse.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Be(cachedResponse.Content);

        _cache.ReceivedCalls().Should().HaveCount(1);
        _innerHandler.CallsToSendAsync.Should().Be(0);
    }

    [Fact]
    public async Task SendAsyncInternal_gets_fresh_response_if_not_cached()
    {
        _cache.GetAsync(Arg.Any<string>(), Cancellation).Returns((byte[])null!);
        _innerHandler.SetupResponse(HttpStatusCode.NotFound, string.Empty);

        HttpRequestMessage request = CreateRequest(HttpMethod.Get);

        _ = await _handler.SendAsyncInternal(request, Cancellation);

        _cache.ReceivedCalls().Should().HaveCount(1);
        _innerHandler.CallsToSendAsync.Should().Be(1);
    }

    [Fact]
    public async Task SendAsyncInternal_returns_error_if_api_throws()
    {
        const string errorMessage = "something unexpected happened";

        _cache.GetAsync(Arg.Any<string>(), Cancellation).Returns((byte[])null!);
        _innerHandler.SetupResponse(HttpStatusCode.InternalServerError, errorMessage);

        HttpRequestMessage request = CreateRequest(HttpMethod.Get);

        HttpResponseMessage? response = null;
        var action = async () => response = await _handler.SendAsyncInternal(request, Cancellation);

        await action.Should().NotThrowAsync<ApiException>();

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain(errorMessage);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method)
    {
        return new(method, "about:blank")
        {
            Content = new StringContent("[1, 2, 3]")
        };
    }

    private async Task<CachedHttpResponse> SetupCachedResponse(HttpRequestMessage request)
    {
        var cacheKey = await CachedHttpClientHandler.GetCacheKey(request, Cancellation);
        var cachedResponse = new CachedHttpResponse(HttpStatusCode.OK, "[ 4, 5, 6 ]");

        byte[] cachedValue = JsonSerializer.SerializeToUtf8Bytes(cachedResponse);

        _cache
            .GetAsync(cacheKey, Cancellation)
            .Returns(cachedValue);

        return cachedResponse;
    }
}

public class TestMessageHandler : HttpMessageHandler
{
    public int CallsToSendAsync { get; set; }

    private readonly List<(HttpStatusCode statusCode, string content)> _expectedResponses;

    public TestMessageHandler()
    {
        CallsToSendAsync = 0;
        _expectedResponses = new();
    }

    public void SetupResponse(HttpStatusCode statusCode, string content)
    {
        _expectedResponses.Add((statusCode, content));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return SendAsyncInternal();
    }

    public async Task<HttpResponseMessage> SendAsyncInternal()
    {
        await Task.CompletedTask;

        (var statusCode, var content) = _expectedResponses[CallsToSendAsync];

        CallsToSendAsync++;

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            throw new ApiException(content, (int)statusCode, "Error", null!, null!);
        }

        return CachedHttpClientHandler.CreateResponse(statusCode, content);
    }
}

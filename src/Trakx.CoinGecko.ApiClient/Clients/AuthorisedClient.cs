namespace Trakx.CoinGecko.ApiClient;

public abstract class AuthorisedClient
{
    protected async Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        HttpRequestMessage httpRequestMessage = new();
        return httpRequestMessage;
    }
}
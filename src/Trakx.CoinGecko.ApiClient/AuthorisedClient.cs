using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Trakx.CoinGecko.ApiClient
{
    internal abstract class AuthorisedClient
    {
        public readonly CoinGeckoApiConfiguration Configuration;
        protected string BaseUrl => Configuration!.BaseUrl;

        protected AuthorisedClient(ClientConfigurator clientConfigurator)
        {
            Configuration = clientConfigurator.ApiConfiguration;
        }

        protected Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
        {
            var msg = new HttpRequestMessage();
            return Task.FromResult(msg);
        }
    }
}

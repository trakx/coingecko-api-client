using Trakx.Utils.Testing;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    public class EnvFileDocumentationUpdater: EnvFileDocumentationUpdaterBase
    {
        public EnvFileDocumentationUpdater(ITestOutputHelper output) : base(output)
        {
        }
    }
}
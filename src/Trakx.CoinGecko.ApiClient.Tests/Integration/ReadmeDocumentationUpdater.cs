using Trakx.Utils.Testing.ReadmeUpdater;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class ReadmeDocumentationUpdater : ReadmeDocumentationUpdaterBase
{
    public ReadmeDocumentationUpdater(ITestOutputHelper output) : base(output)
    {
    }
}

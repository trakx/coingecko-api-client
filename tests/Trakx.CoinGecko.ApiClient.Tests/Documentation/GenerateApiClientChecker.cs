using Trakx.Common.Testing.Documentation.GenerateApiClient;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class GenerateApiClientChecker : GenerateApiClientCheckerBase<CoinGeckoApiConfiguration>
{
    public GenerateApiClientChecker(ITestOutputHelper output) : base(output)
    {
    }
}
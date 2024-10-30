using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class SearchClientTests : CoinGeckoClientTestBase
{
    private readonly ISearchClient _searchClient;

    public SearchClientTests(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
        : base(apiFixture, output)
    {
        _searchClient = ServiceProvider.GetRequiredService<ISearchClient>();
    }

    [Fact]
    public async Task SearchDataAsync_should_return_data_when_passing_valid_symbol()
    {
        var data = await _searchClient.SearchDataAsync("btc");
        data.Should().NotBeNull();
        data.Content.Should().NotBeNull();
        data.Content.Coins.Should().NotBeEmpty();
    }
}

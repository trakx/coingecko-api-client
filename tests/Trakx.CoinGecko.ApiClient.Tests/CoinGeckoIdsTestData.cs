using System.Collections;

namespace Trakx.CoinGecko.ApiClient.Tests;

public class CoinGeckoIdsTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return ["bitcoin"];
        yield return ["ethereum"];
        yield return ["aave"];
        yield return ["pax-gold"];
        yield return ["uma"];
        yield return ["binancecoin"];
        yield return ["cardano"];
        yield return ["theta-token"];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
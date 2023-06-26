using System.Collections;
using System.Collections.Generic;

namespace Trakx.CoinGecko.ApiClient.Tests;

public class CoinGeckoIdsTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "bitcoin" };
        yield return new object[] { "ethereum" };
        yield return new object[] { "aave" };
        yield return new object[] { "pax-gold" };
        yield return new object[] { "uma" };
        yield return new object[] { "binancecoin" };
        yield return new object[] { "cardano" };
        yield return new object[] { "theta-token" };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
using System;
using System.Runtime.Serialization;

namespace Trakx.CoinGecko.ApiClient;

[Serializable]
public class FailedToRetrieveCoinGeckoIdException : Exception
{
    public FailedToRetrieveCoinGeckoIdException(string message)
        : base(message) { }

    protected FailedToRetrieveCoinGeckoIdException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

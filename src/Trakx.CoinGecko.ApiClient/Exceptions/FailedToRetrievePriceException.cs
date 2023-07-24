using System;
using System.Runtime.Serialization;

namespace Trakx.CoinGecko.ApiClient;

[Serializable]
public class FailedToRetrievePriceException : Exception
{
    public FailedToRetrievePriceException(string message)
        : base(message) { }

    protected FailedToRetrievePriceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
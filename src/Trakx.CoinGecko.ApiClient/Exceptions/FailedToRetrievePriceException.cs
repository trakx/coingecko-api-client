using System;

namespace Trakx.CoinGecko.ApiClient;

[Serializable]
public class FailedToRetrievePriceException : Exception
{
    public FailedToRetrievePriceException()
    {
    }

    public FailedToRetrievePriceException(string? message) : base(message)
    {
    }

    public FailedToRetrievePriceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
using System;

namespace Trakx.CoinGecko.ApiClient;

[Serializable]
public class FailedToRetrieveCoinGeckoIdException : Exception
{
    public FailedToRetrieveCoinGeckoIdException()
    {
    }

    public FailedToRetrieveCoinGeckoIdException(string? message) : base(message)
    {
    }

    public FailedToRetrieveCoinGeckoIdException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

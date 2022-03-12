using System;

namespace Trakx.CoinGecko.ApiClient;

public class FailedToRetrievePriceException : Exception
{
    public FailedToRetrievePriceException(string message) 
        : base(message) { }

}
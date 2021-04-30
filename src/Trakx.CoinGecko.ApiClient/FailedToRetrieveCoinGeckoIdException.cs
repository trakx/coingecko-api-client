using System;

namespace Trakx.CoinGecko.ApiClient
{
    public class FailedToRetrieveCoinGeckoIdException : Exception
    {
        public FailedToRetrieveCoinGeckoIdException(string message) 
            : base(message) { }

    }
}

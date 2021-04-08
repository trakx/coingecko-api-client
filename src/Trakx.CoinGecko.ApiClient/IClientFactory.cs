using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trakx.CoinGecko.ApiClient
{
    public  interface IClientFactory
    {

        ICoinsClient CreateCoinsClient();

        ISimpleClient CreateSimpleClient();

    }
}

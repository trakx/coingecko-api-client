using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trakx.CoinGecko.ApiClient
{
    internal partial class GlobalClient : AuthorisedClient, IGlobalClient
    {

        protected readonly int defi_market_cap = 200;
        protected readonly int eth_market_cap = 200;
        protected readonly int defi_to_eth_ratio = 200;
        protected readonly int trading_volume_24h = 200;
        protected readonly int defi_dominance = 200;
        protected readonly int top_coin_name = 200;
        protected readonly int top_coin_dominance = 200;
        protected readonly int data = 200;

    }
}

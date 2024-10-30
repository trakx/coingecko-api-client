using Trakx.Common.Extensions;

namespace Trakx.CoinGecko.ApiClient;

// will soon become CoinGeckoSymbolsClient
public partial class CoinGeckoClient
{
    private async Task<List<CoinList>> GetCoinListInternal(CancellationToken cancellationToken = default)
    {
        var coinList = await _coinsClient.ListAllAsync(cancellationToken: cancellationToken);
        return coinList.Content;
    }

    private async Task<HashSet<string>> GetSupportedQuoteCurrenciesInternal(CancellationToken cancellationToken = default)
    {
        var response = await _simpleClient.Supported_vs_currenciesAsync(cancellationToken);
        var result = new HashSet<string>(response.Content, StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private async Task<List<Coins>> GetCoinsFromSymbolInternal(string symbol, CancellationToken cancellationToken = default)
    {
        var search = await _searchClient.SearchDataAsync(symbol, cancellationToken);
        return search
            ?.Content?.Coins
            ?.Where(p => p.Symbol.EqualsIgnoreCase(symbol))
            ?.ToList()
            ?? [];
    }

    private async Task<string?> GetCoinGeckoIdFromSymbolInternal(string symbol, CancellationToken cancellationToken = default)
    {
        var map = await MapRankedSymbolsToCoinGeckoIds(cancellationToken);
        var id = map.GetValueOrDefault(symbol)?.FirstOrDefault();
        if (id != null) return id;

        var coins = await GetCoinsFromSymbolInternal(symbol, cancellationToken);

        // coins come ordered by market rank desc (i.e. most valuable first)
        return coins?.FirstOrDefault()?.Api_symbol;
    }

    private async Task<SymbolToCoinGeckoIdsMap> MapRankedSymbolsToCoinGeckoIdsInternal(CancellationToken cancellationToken = default)
    {
        // get the highest ranked coin
        var rank = await GetMarketRank(cancellationToken: cancellationToken);

        // rank coin symbols, having the most popular option at the top
        // example: there's a UNI token at #28 market cap and another with rank ~2300
        // there's a really, really, really high chance we want the most popular token
        var rankLookup = rank
            .Where(p => p.CoinSymbol != null && p.CoinId != null)
            .ToLookup(p => p.CoinSymbol!, StringComparer.OrdinalIgnoreCase);

        var map = rankLookup.ToDictionary(
            group => group.Key,
            group => group
                .OrderBy(p => p.MarketCapRank ?? int.MaxValue)
                .Select(p => p.CoinId!).ToArray());

        return map;
    }

    private async Task<SymbolToCoinGeckoIdsMap> GetIdsFromSymbolsInternal(IList<string> symbols, CancellationToken cancellationToken = default)
    {
        // we can get a full "symbol -> coingecko id" map from the market data rank, which is cached for a day
        var fullMap = await MapRankedSymbolsToCoinGeckoIds(cancellationToken);

        var result = new SymbolToCoinGeckoIdsMap(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in symbols)
        {
            var ids = fullMap[symbol];
            if (ids == null)
            {
                // symbol is unranked, search API for the symbol
                var coins = await GetCoinsFromSymbol(symbol, cancellationToken);
                if (coins.IsNullOrEmpty()) continue;
                ids = coins.Select(p => p.Id).ToArray();
            }
            result[symbol] = ids;
        }

        return result;
    }
}

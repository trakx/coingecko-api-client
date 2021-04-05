using Trakx.Utils.Attributes;
using Trakx.Utils.Testing;

namespace Trakx.CoinGecko.ApiClient.Tests
{
    public record Secrets : SecretsBase
    {
        [SecretEnvironmentVariable(nameof(CoinGeckoApiConfiguration), nameof(CoinGeckoApiConfiguration.ApiKey))]
        public string? CoinGeckoApiKey { get; init; }
        [SecretEnvironmentVariable(nameof(CoinGeckoApiConfiguration), nameof(CoinGeckoApiConfiguration.ApiSecret))]
        public string? CoinGeckoApiSecret { get; init; }
    }
    
}
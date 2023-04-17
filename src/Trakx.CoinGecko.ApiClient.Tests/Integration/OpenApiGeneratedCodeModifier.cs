using System.IO;
using Trakx.Common.Infrastructure.Environment.Env;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class OpenApiGeneratedCodeModifier : Trakx.Common.Testing.Documentation.OpenApiGeneratedCodeModifier
{
    public OpenApiGeneratedCodeModifier(ITestOutputHelper output)
        : base(output)
    {
        default(DirectoryInfo).TryWalkBackToRepositoryRoot(out var rootDirectory);

        FilePaths.Add(Path.Combine(rootDirectory!.FullName, "src",
            "Trakx.CoinGecko.ApiClient", "ApiClients.cs"));
    }
}
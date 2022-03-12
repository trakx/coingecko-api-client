using System.IO;
using Trakx.Utils.Extensions;
using Xunit.Abstractions;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

public class OpenApiGeneratedCodeModifier : Trakx.Utils.Testing.OpenApiGeneratedCodeModifier
{
    public OpenApiGeneratedCodeModifier(ITestOutputHelper output)
        : base(output)
    {
        var foundRoot = default(DirectoryInfo).TryWalkBackToRepositoryRoot(out var rootDirectory)!;
        FilePaths.Add(Path.Combine(rootDirectory!.FullName, "src",
            "Trakx.CoinGecko.ApiClient", "ApiClients.cs"));
    }
}
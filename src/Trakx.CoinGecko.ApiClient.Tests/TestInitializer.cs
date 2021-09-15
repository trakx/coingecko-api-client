using System;
using System.Diagnostics;
using System.IO;
using Trakx.Utils.Extensions;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("Trakx.CoinGecko.ApiClient.Tests.TestInitializer", "Trakx.CoinGecko.ApiClient.Tests")]
namespace Trakx.CoinGecko.ApiClient.Tests
{
    public class TestInitializer: XunitTestFramework
    {
        public TestInitializer(IMessageSink messageSink) : base(messageSink)
        {
#if DEBUG
            try
            {
                var envFilePath = ((DirectoryInfo?)default).GetDefaultEnvFilePath();
                DotNetEnv.Env.Load(envFilePath);
                Debug.Assert(Environment.GetEnvironmentVariable("IsEnvFileLoaded")!.Equals("Yes"));
            }
            catch { /**/ }
#endif
        }
    }
}
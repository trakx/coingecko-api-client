using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using Serilog.Filters;
using Xunit;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration
{
    [Collection(nameof(ApiTestCollection))]
    public class CoinGeckoClientTestBase
    {

        protected ServiceProvider ServiceProvider;

        public CoinGeckoClientTestBase(CoinGeckoApiFixture apiFixture)
        {
            ServiceProvider = apiFixture.ServiceProvider;
        }

        protected void EnsureAllJsonElementsWereMapped(object? obj)
        {
            if (obj == null) return;
            else
            {
                var fullName = obj.GetType().FullName;
                if (fullName != null && !fullName.StartsWith("Trakx.")) return;
            }

            var extendedDataFieldInfo = (from property in obj.GetType().GetProperties()
                                         from attribute in property.GetCustomAttributes(typeof(JsonExtensionDataAttribute), true)
                                         select property).FirstOrDefault();

            if (extendedDataFieldInfo != null &&
                extendedDataFieldInfo.PropertyType == typeof(IDictionary<string, object>))
            {
                if (extendedDataFieldInfo.GetValue(obj) is IDictionary<string, object> notMappedValues && notMappedValues.Any())
                {
                    throw new Exception($"The following element(s) must be mapped to the object '{obj.GetType().FullName}': " +
                                        $"{string.Join(",", notMappedValues.Keys)}");
                }
            }

            foreach (var property in obj.GetType().GetProperties().Where(f => f != extendedDataFieldInfo))
            {
                if (property.PropertyType.GetInterface(typeof(IDictionary<,>).Name) != null)
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        foreach (var keyValue in (IDictionary<string, object>)value)
                        {
                            EnsureAllJsonElementsWereMapped(keyValue.Value);
                        }
                    }
                }
                else if (property.PropertyType.GetInterface(typeof(IList<>).Name) != null)
                {
                    var value = property.GetValue(obj);
                    if (value != null)
                    {
                        foreach (var item in (IList)value)
                        {
                            EnsureAllJsonElementsWereMapped(item);
                        }
                    }
                }
                else if (property.PropertyType.FullName != null &&
                         property.PropertyType.FullName.StartsWith("Trakx."))
                {
                    EnsureAllJsonElementsWereMapped(property.GetValue(obj));
                }
            }
        }


    }

    [CollectionDefinition(nameof(ApiTestCollection))]
    public class ApiTestCollection : ICollectionFixture<CoinGeckoApiFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public class CoinGeckoApiFixture : IDisposable
    {

        internal const string LogOutputTemplate = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message} ({SourceContext}){NewLine}{Exception}";

        public ServiceProvider ServiceProvider { get; }

        public CoinGeckoApiFixture()
        {
                var configuration = new CoinGeckoApiConfiguration
            {
                BaseUrl = "https://api.coingecko.com/api/v3"
            };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddCoinGeckoClient(configuration);
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: LogOutputTemplate)
                .MinimumLevel.Debug()
                .Filter.ByExcluding(Matching.FromSource("Microsoft"))
                .CreateLogger()
                .ForContext(MethodBase.GetCurrentMethod()!.DeclaringType);

            serviceCollection.AddSingleton(Log.Logger);

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            ServiceProvider.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
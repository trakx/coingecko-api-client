using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sprache;
using Trakx.Common.Testing.Logging;

namespace Trakx.CoinGecko.ApiClient.Tests.Integration;

[Collection(nameof(ApiTestCollection))]
public class CoinGeckoClientTestBase
{
    protected ServiceProvider ServiceProvider { get; }
    protected ILogger Logger { get; }

    protected CoinGeckoClientTestBase(CoinGeckoApiFixture apiFixture, ITestOutputHelper output)
    {
        Logger = output.SetupLoggingAndGetDefaultLogger();

        apiFixture.ServiceCollection.SetupTestLoggerProvider(builder => builder.AddXUnit(output));

        ServiceProvider = apiFixture.ServiceProvider;
    }

    protected static void EnsureAllJsonElementsWereMapped(object? obj)
    {
        if (obj == null) return;

        var fullName = obj.GetType().FullName;
        if (fullName != null && !fullName.StartsWith("Trakx.")) return;

        var extendedDataFieldInfo = (from property in obj.GetType().GetProperties()
                                     from attribute in property.GetCustomAttributes(typeof(JsonExtensionDataAttribute), true)
                                     select property).FirstOrDefault();

        if (extendedDataFieldInfo?.GetValue(obj) is IDictionary<string, object> notMappedValues && notMappedValues.Any())
        {
            throw new Exception($"The following element(s) must be mapped to the object '{obj.GetType().FullName}': " +
                                $"{string.Join(",", notMappedValues.Keys)}");
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

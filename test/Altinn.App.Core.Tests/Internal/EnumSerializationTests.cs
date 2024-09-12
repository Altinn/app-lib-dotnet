using System.Reflection;
using System.Text.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.Internal;

public class EnumSerializationTests
{
    [Fact]
    public void EnsureSerializationPolicyOnEnums()
    {
        var assemblies = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(a => a.FullName is not null && a.FullName.StartsWith("Altinn.App.Core"));
        var enumTypes = GetEnumTypesFromAssemblies(assemblies);
        var nonCompliantEnums = new List<string>();

        foreach (var enumType in enumTypes)
        {
            var jsonConverterAttribute = enumType.GetCustomAttribute<JsonConverterAttribute>();
            if (jsonConverterAttribute == null)
            {
                string enumInfo = enumType.FullName ?? GetEnumInfo(enumType);
                nonCompliantEnums.Add(enumInfo);
            }
        }

        Assert.Empty(nonCompliantEnums);
    }

    private static string GetEnumInfo(Type enumType)
    {
        var assemblyName = enumType.Assembly.GetName().Name;
        var namespaceName = enumType.Namespace ?? "<No Namespace>";
        var typeName = enumType.Name;
        return $"{typeName} (in {namespaceName}, Assembly: {assemblyName})";
    }

    private static IEnumerable<Type> GetEnumTypesFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsEnum && t.Namespace != null && t.Namespace.StartsWith("Altinn.App.Core"));
    }
}

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
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Altinn.App.Core"));
        var enumTypes = GetEnumTypesFromAssemblies(assemblies);
        var nonCompliantEnums = new List<string>();

        foreach (var enumType in enumTypes)
        {
            var jsonConverterAttribute = enumType.GetCustomAttribute<JsonConverterAttribute>();
            if (jsonConverterAttribute == null)
            {
                nonCompliantEnums.Add(enumType.FullName);
            }
        }

        Assert.Empty(nonCompliantEnums);
    }

    private IEnumerable<Type> GetEnumTypesFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsEnum && t.Namespace != null && t.Namespace.StartsWith("Altinn.App.Core"));
    }
}

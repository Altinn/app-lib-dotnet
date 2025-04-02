using System.Collections.Immutable;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

internal static class ModelPathNodeTools
{
    public record Property(
        ImmutableArray<string> CSharp,
        ImmutableArray<string> Json,
        ModelPathNode Node,
        ImmutableArray<Property> Children
    );

    public static ImmutableArray<Property> GetListProperties(ModelPathNode rootNode)
    {
        var properties = new List<Property>();
        GetListPropertiesRecursive(ImmutableArray<string>.Empty, ImmutableArray<string>.Empty, rootNode, properties);
        return properties.ToImmutableArray();
    }

    private static void GetListPropertiesRecursive(
        ImmutableArray<string> jPrefix,
        ImmutableArray<string> csPrefix,
        ModelPathNode node,
        List<Property> properties
    )
    {
        foreach (var child in node.Properties.Where(c => !c.Properties.IsDefaultOrEmpty))
        {
            if (child.ListType is null)
            {
                var jsonPath = jPrefix.Add(child.JsonPath);
                var cSharp = csPrefix.Add(child.CSharpPath);
                GetListPropertiesRecursive(jsonPath, cSharp, child, properties);
            }
            else
            {
                var childProperties = new List<Property>();
                var jsonPath = ImmutableArray.Create(child.JsonPath);
                var cSharp = ImmutableArray.Create(child.CSharpPath);

                GetListPropertiesRecursive(jsonPath, cSharp, child, childProperties);
                properties.Add(new Property(cSharp, jsonPath, child, childProperties.ToImmutableArray()));
            }
        }
    }
}

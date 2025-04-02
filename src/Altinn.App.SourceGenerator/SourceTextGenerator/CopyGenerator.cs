using System.Text;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

internal static class CopyGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode, string generatedWrapperClassName)
    {
        // TODO: Give real implementation that does not go through json
        builder.Append(
            $$"""

                /// <inheritdoc />
                public IFormDataWrapper Copy()
                {
                    return new {{generatedWrapperClassName}}(CopyRecursive(_dataModel));
                }
            
            """
        );

        GenerateCopyRecursive(builder, rootNode, new());
    }

    private static void GenerateCopyRecursive(StringBuilder builder, ModelPathNode node, HashSet<string> classNames)
    {
        if (node.ListType is not null && classNames.Add($"{node.ListType}<{node.Type}>"))
        {
            GenerateCopyList(builder, node);
        }
        if (!classNames.Add(node.Type))
        {
            // Ignore repeated types
            return;
        }
        builder.Append(
            $$"""

                [return: NotNullIfNotNull("data")]
                private static global::{{node.Type}}? CopyRecursive(
                    global::{{node.Type}}? data
                )
                {
                    if (data is null)
                    {
                        return null;
                    }

            
            """
        );
        if (node.Properties.IsDefaultOrEmpty)
        {
            builder.Append("        return new();\r\n    }\r\n");
        }
        else
        {
            builder.Append("        return new()\r\n        {\r\n");
            foreach (var property in node.Properties)
            {
                builder.Append(
                    property.Properties.IsDefaultOrEmpty
                        ? $"            {property.CSharpPath} = data.{property.CSharpPath},\r\n"
                        : $"            {property.CSharpPath} = CopyRecursive(data.{property.CSharpPath}),\r\n"
                );
            }

            builder.Append("        };\r\n    }\r\n");
        }

        foreach (var recursiveChild in node.Properties.Where(c => !c.Properties.IsDefaultOrEmpty))
        {
            GenerateCopyRecursive(builder, recursiveChild, classNames);
        }
    }

    private static void GenerateCopyList(StringBuilder builder, ModelPathNode node)
    {
        builder.Append(
            $$"""

                [return: NotNullIfNotNull("list")]
                private static global::{{node.ListType}}<global::{{node.Type}}?>? CopyRecursive(
                    global::{{node.ListType}}<global::{{node.Type}}?>? list
                )
                {
                    if (list is null)
                    {
                        return null;
                    }

                    return [.. list.Select(CopyRecursive)];
                }
            
            """
        );
    }
}

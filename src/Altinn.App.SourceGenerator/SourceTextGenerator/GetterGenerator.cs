using System.Text;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

/// <summary>
/// Generate the implementation for the GetRaw method.
/// </summary>
public static class GetterGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        if (rootNode.Children.IsDefaultOrEmpty)
        {
            builder.Append(
                """

                    /// <inheritdoc />
                    public object? GetRaw(ReadOnlySpan<char> path) => null;

                """
            );
            return;
        }
        builder.Append(
            """

                /// <inheritdoc />
                public object? GetRaw(ReadOnlySpan<char> path)
                {
                    if (path.IsEmpty)
                    {
                        return null;
                    }

                    return GetRecursive(_dataModel, path, 0);
                }

            """
        );

        GenerateRecursive(builder, rootNode, new HashSet<string>());
    }

    private static void GenerateRecursive(
        StringBuilder builder,
        ModelPathNode modelPathNode,
        HashSet<string> generatedTypes
    )
    {
        if (modelPathNode.Children.IsDefaultOrEmpty)
        {
            // Do not generate for primitive types
            return;
        }

        if (modelPathNode.ListType != null && generatedTypes.Add($"{modelPathNode.ListType}<{modelPathNode.Type}>"))
        {
            builder.Append(
                $$"""

                    private static object? GetRecursive(
                        global::{{modelPathNode.ListType}}<global::{{modelPathNode.Type}}?>? model,
                        ReadOnlySpan<char> path,
                        int offset
                    )
                    {
                        int index = PathHelper.GetIndex(path, offset, out int nextOffset);
                        if (index < 0 || index >= model?.Count)
                        {
                            // throw new IndexOutOfRangeException($"Index {index} is out of range for list of length {model.Count}.");
                            return null;
                        }

                        return GetRecursive(model?[index], path, nextOffset);
                    }
                
                """
            );
        }

        if (!generatedTypes.Add(modelPathNode.Type))
        {
            // Do not generate the same type twice
            return;
        }

        builder.Append(
            $$"""

                private static object? GetRecursive(
                    global::{{modelPathNode.Type}}? model,
                    ReadOnlySpan<char> path,
                    int offset
                )
                {
                    return PathHelper.GetNextSegment(path, offset, out int nextOffset) switch
                    {
            
            """
        );
        foreach (var child in modelPathNode.Children)
        {
            builder.Append(
                child.Children.IsDefaultOrEmpty
                    ? $"            \"{child.JsonPath}\" when nextOffset is -1 => model?.{child.CSharpPath},\r\n"
                    : $"            \"{child.JsonPath}\" => GetRecursive(model?.{child.CSharpPath}, path, nextOffset),\r\n"
            );
        }

        builder.Append(
            """
                        "" => model,
                        // _ => throw new ArgumentException("{path} is not a valid path."),
                        _ => null,
                    };
                }

            """
        );

        foreach (var child in modelPathNode.Children)
        {
            GenerateRecursive(builder, child, generatedTypes);
        }
    }
}

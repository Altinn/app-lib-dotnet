using System.Text;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

/// <summary>
/// Generate the implementation for the GetRaw method.
/// </summary>
public static class RemoveGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        if (rootNode.Children.IsDefaultOrEmpty)
        {
            builder.Append(
                """

                    /// <inheritdoc />
                    public void RemoveField(ReadOnlySpan<char> path, RowRemovalOption rowRemovalOption) { }

                """
            );
            return;
        }
        builder.Append(
            """

                /// <inheritdoc />
                public void RemoveField(ReadOnlySpan<char> path, RowRemovalOption rowRemovalOption)
                {
                    if (path.IsEmpty)
                    {
                        return;
                    }

                    RemoveRecursive(_dataModel, path, 0, rowRemovalOption);
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

                    private static void RemoveRecursive(
                        global::{{modelPathNode.ListType}}<global::{{modelPathNode.Type}}?>? model,
                        ReadOnlySpan<char> path,
                        int offset,
                        RowRemovalOption rowRemovalOption
                    )
                    {
                        int index = PathHelper.GetIndex(path, offset, out int nextOffset);
                        if (index < 0 || index >= model?.Count || model is null)
                        {
                            return;
                        }
                        if (nextOffset == -1)
                        {
                            switch (rowRemovalOption)
                            {
                                case RowRemovalOption.DeleteRow:
                                    model.RemoveAt(index);
                                    break;
                                case RowRemovalOption.SetToNull:
                                    model[index] = null;
                                    break;
                            }
                        }
                        else
                        {
                            RemoveRecursive(model?[index], path, nextOffset, rowRemovalOption);
                        }
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

                private static void RemoveRecursive(
                    global::{{modelPathNode.Type}}? model,
                    ReadOnlySpan<char> path,
                    int offset,
                    RowRemovalOption rowRemovalOption
                )
                {
                    if (model is null)
                    {
                        return;
                    }
                    switch (PathHelper.GetNextSegment(path, offset, out int nextOffset))
                    {
            
            """
        );
        foreach (var child in modelPathNode.Children)
        {
            if (
                child is
                {
                    CSharpPath: "AltinnRowId",
                    Type: "System.Guid",
                    Children.IsDefaultOrEmpty: true,
                    JsonPath: "altinnRowId"
                }
            )
            {
                // We altinnRowId isn't nullable, and it is set on its own schedule.
                continue;
            }

            builder.Append(
                $"""
                            case "{child.JsonPath}" when nextOffset is -1:
                                model.{child.CSharpPath} = null;
                                break;
                
                """
            );
            if (!child.Children.IsDefaultOrEmpty)
            {
                builder.Append(
                    $"""
                                case "{child.JsonPath}":
                                    RemoveRecursive(model.{child.CSharpPath}, path, nextOffset, rowRemovalOption);
                                    break;
                    
                    """
                );
            }
        }

        builder.Append(
            """
                        default:
                            // throw new ArgumentException("{path} is not a valid path.");
                            return;
                    }
                }

            """
        );

        foreach (var child in modelPathNode.Children)
        {
            GenerateRecursive(builder, child, generatedTypes);
        }
    }
}

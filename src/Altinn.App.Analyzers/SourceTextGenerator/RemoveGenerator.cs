using System.Text;

namespace Altinn.App.Analyzers.SourceTextGenerator;

internal static class RemoveGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        if (rootNode.Properties.Count == 0)
        {
            builder.Append(
                """

                    /// <inheritdoc />
                    public void RemoveField(global::System.ReadOnlySpan<char> path, global::Altinn.App.Core.Helpers.RowRemovalOption rowRemovalOption) { }

                """
            );
            return;
        }
        builder.Append(
            """

                /// <inheritdoc />
                public void RemoveField(global::System.ReadOnlySpan<char> path, global::Altinn.App.Core.Helpers.RowRemovalOption rowRemovalOption)
                {
                    if (path.IsEmpty)
                    {
                        return;
                    }

                    RemoveRecursive(_dataModel, path, 0, rowRemovalOption);
                }

            """
        );

        GenerateRecursive(builder, rootNode, new HashSet<string>(StringComparer.Ordinal));
    }

    private static void GenerateRecursive(
        StringBuilder builder,
        ModelPathNode modelPathNode,
        HashSet<string> generatedTypes
    )
    {
        if (modelPathNode.Properties.Count == 0)
        {
            // Do not generate for primitive types
            return;
        }

        if (modelPathNode.ListType != null && generatedTypes.Add($"{modelPathNode.ListType}<{modelPathNode.TypeName}>"))
        {
            builder.Append(
                $$"""

                    private static void RemoveRecursive(
                        {{modelPathNode.ListType}}? model,
                        global::System.ReadOnlySpan<char> path,
                        int offset,
                        global::Altinn.App.Core.Helpers.RowRemovalOption rowRemovalOption
                    )
                    {
                        if (model is null)
                        {
                            return;
                        }
                        int index = GetIndex(path, offset, out int nextOffset);
                        if (index < 0 || index >= model.Count)
                        {
                            return;
                        }
                        if (nextOffset == -1)
                        {
                            switch (rowRemovalOption)
                            {
                                case global::Altinn.App.Core.Helpers.RowRemovalOption.DeleteRow:
                                    model.RemoveAt(index);
                                    break;
                                case global::Altinn.App.Core.Helpers.RowRemovalOption.SetToNull:
                                    model[index] = default!;
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

        if (!generatedTypes.Add(modelPathNode.TypeName))
        {
            // Do not generate the same type twice
            return;
        }
        builder.Append(
            $$"""

                private static void RemoveRecursive(
                    {{modelPathNode.TypeName}}? model,
                    global::System.ReadOnlySpan<char> path,
                    int offset,
                    global::Altinn.App.Core.Helpers.RowRemovalOption rowRemovalOption
                )
                {
                    if (model is null)
                    {
                        return;
                    }
                    switch (GetNextSegment(path, offset, out int nextOffset))
                    {

            """
        );
        foreach (var child in modelPathNode.Properties)
        {
            if (child.IsAltinnRowId())
            {
                // altinnRowId isn't nullable, and it is set on its own schedule.
                continue;
            }

            builder.Append(
                $"""
                            case "{child.JsonName}" when nextOffset is -1:
                                model.{child.CSharpName} = default;
                                break;

                """
            );
            if (child.Properties.Count != 0)
            {
                builder.Append(
                    $"""
                                case "{child.JsonName}":
                                    RemoveRecursive(model.{child.CSharpName}, path, nextOffset, rowRemovalOption);
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

        foreach (var child in modelPathNode.Properties)
        {
            GenerateRecursive(builder, child, generatedTypes);
        }
    }
}

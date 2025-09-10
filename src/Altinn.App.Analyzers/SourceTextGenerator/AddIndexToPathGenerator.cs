using System.Text;

namespace Altinn.App.Analyzers.SourceTextGenerator;

internal static class AddIndexToPathGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        builder.Append(
            $$"""

                /// <inheritdoc />
                public global::System.ReadOnlySpan<char> AddIndexToPath(global::System.ReadOnlySpan<char> path, global::System.ReadOnlySpan<int> rowIndexes, global::System.Span<char> buffer)
                {
                    var bufferOffset = 0;
                    var pathOffset = 0;

                    AddIndexToPathRecursive_{{rootNode.Name}}(
                        path,
                        pathOffset,
                        rowIndexes,
                        buffer,
                        ref bufferOffset
                    );

                    return buffer[..bufferOffset];
                }

            """
        );
        GenerateRecursiveMethod(builder, rootNode, new HashSet<string>(StringComparer.Ordinal));
    }

    private static void GenerateRecursiveMethod(
        StringBuilder builder,
        ModelPathNode node,
        HashSet<string> generatedTypeNames
    )
    {
        if (!generatedTypeNames.Add(node.TypeName))
        {
            // Already generated this method
            return;
        }
        builder.Append(
            $$"""

                private void AddIndexToPathRecursive_{{node.Name}}(
                    global::System.ReadOnlySpan<char> path,
                    int pathOffset,
                    global::System.ReadOnlySpan<int> rowIndexes,
                    global::System.Span<char> buffer,
                    ref int bufferOffset
                )
                {
                    if (bufferOffset > 0)
                    {
                        buffer[bufferOffset++] = '.';
                    }
                    var segment = GetNextSegment(path, pathOffset, out pathOffset);
                    switch (segment)
                    {

            """
        );

        foreach (var child in node.Properties.Where(IsRelevantForRecursion))
        {
            builder.Append(
                $$"""
                            case "{{child.JsonName}}":
                                segment.CopyTo(buffer.Slice(bufferOffset));
                                bufferOffset += {{child.JsonName.Length}};

                """
            );
            if (child.ListType is not null)
            {
                builder.Append(
                    """

                                    if (pathOffset != -1 && pathOffset < path.Length && path[pathOffset] == '[')
                                    {
                                        // Copy index from path to buffer
                                        GetIndex(path, pathOffset, out int nextPathIndexOffset);
                                        path.Slice(pathOffset, nextPathIndexOffset).CopyTo(buffer.Slice(bufferOffset));
                                        bufferOffset += nextPathIndexOffset - pathOffset;
                                        pathOffset = nextPathIndexOffset;
                                        rowIndexes = default;
                                    }
                                    else if (rowIndexes.Length >= 1)
                                    {
                                        // Write index from rowIndexes to buffer
                                        buffer[bufferOffset++] = '[';
                                        rowIndexes[0].TryFormat(buffer.Slice(bufferOffset), out int charsWritten);
                                        bufferOffset += charsWritten;
                                        buffer[bufferOffset++] = ']';
                                        rowIndexes = rowIndexes.Slice(1);
                                    }
                                    else
                                    {
                                        // No index to write, return an empty path for error handling
                                        bufferOffset = 0;
                                        return;
                                    }

                    """
                );
            }
            builder.Append(
                $$"""
                                if (pathOffset != -1)
                                {
                                    AddIndexToPathRecursive_{{child.Name}}(
                                        path,
                                        pathOffset,
                                        rowIndexes,
                                        buffer,
                                        ref bufferOffset
                                    );
                                }
                                return;

                """
            );
        }

        foreach (var child in node.Properties.Where(c => !IsRelevantForRecursion(c)))
        {
            builder.Append(
                $$"""
                            case "{{child.JsonName}}":
                                segment.CopyTo(buffer.Slice(bufferOffset));
                                bufferOffset += {{child.JsonName.Length}};
                                return;

                """
            );
        }

        builder.Append(
            """
                        default:
                            bufferOffset = 0;
                            return;
                    }
                }

            """
        );
        foreach (var child in node.Properties.Where(c => IsRelevantForRecursion(c)))
        {
            GenerateRecursiveMethod(builder, child, generatedTypeNames);
        }
    }

    private static bool IsRelevantForRecursion(ModelPathNode node)
    {
        return node.Properties.Count != 0;
    }
}

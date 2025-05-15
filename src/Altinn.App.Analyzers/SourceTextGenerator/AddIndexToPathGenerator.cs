using System.Text;

namespace Altinn.App.Analyzers.SourceTextGenerator;

internal static class AddIndexToPathGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        builder.Append(
            $$"""

                /// <inheritdoc />
                public ReadOnlySpan<char> AddIndexToPath(ReadOnlySpan<char> path, ReadOnlySpan<int> rowIndexes, Span<char> buffer)
                {
                    var bufferOffset = 0;
                    var pathOffset = 0;

                    AddIndexToPathRecursive_{{rootNode.Name}}(
                        buffer,
                        path,
                        rowIndexes,
                        ref bufferOffset,
                        ref pathOffset
                    );

                    return buffer[..bufferOffset];
                }

            """
        );
        GenerateRecursiveMethod(builder, rootNode, []);
    }

    private static void GenerateRecursiveMethod(StringBuilder builder, ModelPathNode node, HashSet<string> methods)
    {
        if (!methods.Add(node.TypeName))
        {
            // Already generated this method
            return;
        }
        builder.Append(
            $$"""

                private void AddIndexToPathRecursive_{{node.Name}}(
                    Span<char> buffer,
                    ReadOnlySpan<char> path,
                    ReadOnlySpan<int> rowIndexes,
                    ref int bufferOffset,
                    ref int pathOffset
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

                                    if (path.Length <= pathOffset && path[pathOffset] == '[')
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
                                AddIndexToPathRecursive_{{child.Name}}(
                                    buffer,
                                    path,
                                    rowIndexes,
                                    ref bufferOffset,
                                    ref pathOffset
                                );
                                break;

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
                                pathOffset += {{child.JsonName.Length}};

                                break;

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
            GenerateRecursiveMethod(builder, child, methods);
        }
    }

    private static bool IsRelevantForRecursion(ModelPathNode node)
    {
        return node.Properties.Count != 0;
    }
}

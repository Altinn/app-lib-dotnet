using System.Collections.Immutable;
using System.Text;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

public static class TryAddIndexToPathGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        var listProperties = ModelPathNodeTools.GetListProperties(rootNode);
        if (listProperties.IsDefaultOrEmpty)
        {
            builder.Append(
                """

                    /// <inheritdoc />
                    public bool TryAddIndexToPath(
                        ReadOnlySpan<char> path,
                        ReadOnlySpan<int> rowIndexes,
                        Span<char> buffer,
                        out ReadOnlySpan<char> indexedPath
                    )
                    {
                        indexedPath = path;
                        return true;
                    }

                """
            );
            return;
        }
        builder.Append(
            """

                /// <inheritdoc />
                public bool TryAddIndexToPath(
                    ReadOnlySpan<char> path,
                    ReadOnlySpan<int> rowIndexes,
                    Span<char> buffer,
                    out ReadOnlySpan<char> indexedPath
                )
                {
                    if (path.IsEmpty)
                    {
                        indexedPath = path;
                        return false;
                    }

                    return TryAddIndexToPathRecursive(
                        _dataModel,
                        path,
                        rowIndexes,
                        buffer,
                        out indexedPath
                    );
                }

            """
        );
        GenerateTryAddIndexToPathRecursive(builder, listProperties, rootNode, new HashSet<string>());
    }

    private static void GenerateTryAddIndexToPathRecursive(
        StringBuilder builder,
        ImmutableArray<ModelPathNodeTools.Property> listProperties,
        ModelPathNode node,
        HashSet<string> classNames
    )
    {
        {
            builder.Append(
                $$"""

                    private static void TryAddIndexToPathRecursive({{node.Type}} dataModel, bool initialize)
                    {
                
                """
            );

            var groupIndex = -1;

            foreach (var property in listProperties)
            {
                groupIndex++;
                builder.Append(
                    $$"""
                            if (dataModel.{{(string.Join("?.", property.CSharp))}} is { } group{{groupIndex}})
                            {
                                foreach (var row in group{{groupIndex}})
                                {
                                    if (row is not null)
                                    {
                                        TryAddIndexToPathRecursive(row, initialize);
                                    }
                                }
                            }
                    
                    """
                );
            }
            builder.Append(
                """
                    }

                """
            );

            foreach (var property in listProperties)
            {
                GenerateTryAddIndexToPathRecursive(builder, property.Children, property.Node, classNames);
            }
        }
    }
}

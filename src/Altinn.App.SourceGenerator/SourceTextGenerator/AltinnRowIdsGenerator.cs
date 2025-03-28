using System.Collections.Immutable;
using System.Text;

namespace Altinn.App.SourceGenerator.SourceTextGenerator;

public static class AltinnRowIdsGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        var listProperties = ModelPathNodeTools.GetListProperties(rootNode);
        if (listProperties.IsDefaultOrEmpty)
        {
            builder.Append(
                """

                    /// <inheritdoc />
                    public void RemoveAltinnRowIds() { }

                    /// <inheritdoc />
                    public void InitializeAltinnRowIds() { }

                """
            );

            return;
        }
        builder.Append(
            """

                /// <inheritdoc />
                public void RemoveAltinnRowIds()
                {
                    SetAltinnRowIds(_dataModel, initialize: false);
                }

                /// <inheritdoc />
                public void InitializeAltinnRowIds()
                {
                    SetAltinnRowIds(_dataModel, initialize: true);
                }

            """
        );

        GenerateSetAltinnRowIds(builder, rootNode, listProperties.ToImmutableArray());
    }

    private static void GenerateSetAltinnRowIds(
        StringBuilder builder,
        ModelPathNode node,
        ImmutableArray<ModelPathNodeTools.Property> listProperties
    )
    {
        builder.Append(
            $$"""

                private static void SetAltinnRowIds({{node.Type}} dataModel, bool initialize)
                {
            
            """
        );

        var groupIndex = -1;
        var altinnRowId = node.Children.FirstOrDefault(IsAltinnRowId);
        if (altinnRowId is not null)
        {
            builder.Append(
                $$"""
                        dataModel.{{altinnRowId.CSharpPath}} = initialize ? Guid.NewGuid() : Guid.Empty;
                
                """
            );
        }

        foreach (var property in listProperties.Where(p => p.Node.Children.Any(IsAltinnRowId)))
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
                                    SetAltinnRowIds(row, initialize);
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
            GenerateSetAltinnRowIds(builder, property.Node, property.Children);
        }
    }

    private static bool IsAltinnRowId(ModelPathNode c)
    {
        return c is { JsonPath: "altinnRowId", CSharpPath: "AltinnRowId", Type: "System.Guid" };
    }
}

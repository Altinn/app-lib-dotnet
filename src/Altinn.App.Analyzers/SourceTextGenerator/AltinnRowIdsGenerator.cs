using System.Text;

namespace Altinn.App.Analyzers.SourceTextGenerator;

internal static class AltinnRowIdsGenerator
{
    public static void Generate(StringBuilder builder, ModelPathNode rootNode)
    {
        var property = GetListProperties(rootNode);
        if (property?.Children is not { Count: > 0 })
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

        GenerateSetAltinnRowIds(builder, property, []);
    }

    private static void GenerateSetAltinnRowIds(
        StringBuilder builder,
        PropertyWithListChildren property,
        HashSet<string> classes
    )
    {
        if (!classes.Add(property.Node.TypeName))
        {
            return;
        }
        builder.Append(
            $$"""

                private static void SetAltinnRowIds({{property.Node.TypeName}} dataModel, bool initialize)
                {

            """
        );

        foreach (var child in property.Children)
        {
            if (child.IsRowId)
            {
                builder.Append(
                    $$"""
                            dataModel.{{child.Node.CSharpName}} = initialize ? Guid.NewGuid() : Guid.Empty;

                    """
                );
            }
            else if (child.Node.ListType is null)
            {
                builder.Append(
                    $$"""
                            if(dataModel.{{child.Node.CSharpName}} is not null)
                            {
                                SetAltinnRowIds(dataModel.{{child.Node.CSharpName}}, initialize);
                            }

                    """
                );
            }
            else
            {
                builder.Append(
                    $$"""
                            if(dataModel.{{child.Node.CSharpName}} is not null)
                            {
                                foreach (var item in dataModel.{{child.Node.CSharpName}})
                                {
                                    if (item is not null)
                                    {
                                        SetAltinnRowIds(item, initialize);
                                    }
                                }
                            }

                    """
                );
            }
        }
        builder.Append(
            """
                }

            """
        );

        foreach (var child in property.Children)
        {
            if (!child.IsRowId)
                GenerateSetAltinnRowIds(builder, child, classes);
        }
    }

    private static bool IsAltinnRowId(ModelPathNode c)
    {
        return c is { JsonName: "altinnRowId", CSharpName: "AltinnRowId", TypeName: "global::System.Guid" };
    }

    private sealed record PropertyWithListChildren(
        ModelPathNode Node,
        List<PropertyWithListChildren> Children,
        bool IsRowId
    );

    private static PropertyWithListChildren? GetListProperties(ModelPathNode node)
    {
        var children = node.Properties.Select(GetListProperties).OfType<PropertyWithListChildren>().ToList();
        var isAltinnRowId = IsAltinnRowId(node);
        if (children.Count == 0 && !isAltinnRowId)
        {
            return null;
        }

        return new PropertyWithListChildren(node, children, isAltinnRowId);
    }
}

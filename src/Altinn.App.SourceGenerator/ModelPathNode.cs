using System.Collections.Immutable;
using System.Diagnostics;

namespace Altinn.App.SourceGenerator;

[DebuggerDisplay("{JsonPath}{ListType}[{Children.Length} children]")]
[DebuggerTypeProxy(typeof(DebuggerDisplay))]
public record ModelPathNode
{
    public ModelPathNode(
        string cSharpPath,
        string jsonPath,
        string type,
        ImmutableArray<ModelPathNode> children = default,
        string? listType = null
    )
    {
        CSharpPath = cSharpPath;
        JsonPath = jsonPath;
        ListType = listType;
        Type = type;
        Children = children.IsDefault ? ImmutableArray<ModelPathNode>.Empty : children;
    }

    public ImmutableArray<ModelPathNode> Children { get; init; }
    public string Name => Type.Replace('+', '_').Replace('.', '_').Replace("global::", string.Empty);
    public string JsonPath { get; init; }
    public string CSharpPath { get; init; }
    public string? ListType { get; init; }
    public string Type { get; init; }

    private class DebuggerDisplay(ModelPathNode modelPathNode)
    {
        public bool IsList => modelPathNode.ListType is not null;
        public string JsonPath => modelPathNode.JsonPath;
        public string Type => modelPathNode.Type;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ImmutableArray<ModelPathNode> Children => modelPathNode.Children;
    }
};

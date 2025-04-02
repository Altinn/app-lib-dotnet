using System.Collections.Immutable;
using System.Diagnostics;

namespace Altinn.App.SourceGenerator;

/// <summary>
/// Node used to represent the shape of a model for the purpose of generating code.
/// Somewhat similar to a JSON schema.
/// </summary>
[DebuggerDisplay("{JsonPath}{ListType}[{Properties.Length} children]")]
public record ModelPathNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPathNode"/> class.
    /// </summary>
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
        Properties = children.IsDefault ? ImmutableArray<ModelPathNode>.Empty : children;
    }

    /// <summary>
    /// The sub properties of this node.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public ImmutableArray<ModelPathNode> Properties { get; init; }

    /// <summary>
    /// The full type name with safe characters for C# identifier name
    /// </summary>
    public string Name => Type.Replace('+', '_').Replace('.', '_');

    /// <summary>
    /// The name used in json to access this property. The [JsonPropertyName("")] value.
    /// </summary>
    public string JsonPath { get; init; }

    /// <summary>
    /// The name used in C# to access this property. Used for direct access in source generated code.
    /// </summary>
    public string CSharpPath { get; init; }

    /// <summary>
    /// If this is a list property, this is the type of the list. (eg System.Collections.Generic.List)
    /// </summary>
    /// <remarks>
    /// We assume this is a subtype of <see cref="ICollection{T}"/>
    /// </remarks>
    public string? ListType { get; init; }

    /// <summary>
    /// The FullName of the type of the property.
    /// </summary>
    public string Type { get; init; }
};

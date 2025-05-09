using System.Diagnostics;
using Altinn.App.Analyzers.Utils;

namespace Altinn.App.Analyzers;

/// <summary>
/// Node used to represent the shape of a model for the purpose of generating code.
/// Somewhat similar to a JSON schema.
/// </summary>
[DebuggerDisplay("{_debugDisplayString}")]
public record ModelPathNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPathNode"/> class.
    /// </summary>
    public ModelPathNode(
        string cSharpName,
        string jsonName,
        string typeName,
        EquatableArray<ModelPathNode>? properties = null,
        string? listType = null
    )
    {
        CSharpName = cSharpName;
        JsonName = jsonName;
        ListType = listType;
        TypeName = typeName;
        Properties = properties ?? EquatableArray<ModelPathNode>.Empty;
    }

    /// <summary>
    /// The full type name with safe characters for C# identifier name
    /// </summary>
    public string Name =>
        TypeName
            .Replace('+', '_')
            .Replace('.', '_')
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace("?", "")
            .Replace("global::", "");

    /// <summary>
    /// The name used in json to access this property. The [JsonPropertyName("")] value.
    /// </summary>
    public string JsonName { get; init; }

    /// <summary>
    /// The name used in C# to access this property. Used for direct access in source generated code.
    /// </summary>
    public string CSharpName { get; init; }

    /// <summary>
    /// The FullName of the type of the property or element of list.
    /// </summary>
    public string TypeName { get; init; }

    /// <summary>
    /// If this is a list property, this is the type of the list. (eg System.Collections.Generic.List)
    /// </summary>
    /// <remarks>
    /// We assume this is a subtype of <see cref="ICollection{T}"/>
    /// </remarks>
    public string? ListType { get; init; }

    /// <summary>
    /// The sub properties of this node.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public EquatableArray<ModelPathNode> Properties { get; init; }

    private string _debugDisplayString =>
        $"{JsonName}{(ListType is null ? "" : "[]")} with {Properties.Count} children";
};

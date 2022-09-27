using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Expressions;


/// <summary>
/// Inteface to be able to handle all component groups the same way.
/// </summary>
/// <remarks>
/// Includes <see cref="Children" /> and other properties that might be
/// null for some types of components. We include them at Base level,
/// because they have a common meaning and having a separate level
/// complicates client the codes that traverses components.
/// </remarks>
public class BaseComponent
{
    /// <summary>
    /// Constructor that ensures n
    /// </summary>
    public BaseComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<BaseComponent>? children, LayoutExpression? hidden, LayoutExpression? required)
    {
        Id = id;
        Type = type;
        DataModelBindings = dataModelBindings ?? ImmutableDictionary<string, string>.Empty;
        Hidden = hidden;
        Required = required;
        Children = children ?? Enumerable.Empty<BaseComponent>();
        foreach (var child in Children)
        {
            child.Parent = this;
        }
    }
    /// <summary>
    /// ID of the component (or pagename for pages)
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Get the page for the component
    /// </summary>
    public string Page
    {
        get
        {
            //Get the Id of the first component without a parent.
            return Parent?.Page ?? Id;
        }
    }

    /// <summary>
    /// Component type
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Layout Expression that can be evaluated to see if component should be hidden
    /// </summary>
    public LayoutExpression? Hidden { get; }

    /// <summary>
    /// Layout Expression that can be evaluated to see if component should be required
    /// </summary>
    public LayoutExpression? Required { get; }

    /// <summary>
    /// Data model bindings for the component or group
    /// </summary>
    public IReadOnlyDictionary<string, string> DataModelBindings { get; }

    /// <summary>
    /// 
    /// </summary>
    public BaseComponent? Parent { get; internal set; }

    /// <summary>
    /// The children in this group/page
    /// </summary>
    public IEnumerable<BaseComponent> Children { get; internal set; }
}


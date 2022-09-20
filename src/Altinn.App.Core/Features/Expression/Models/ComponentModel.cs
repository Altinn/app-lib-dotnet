using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Expression;

[JsonConverter(typeof(ComponentModelConverter))]
public class ComponentModel
{
    public Dictionary<string, PageComponent> Pages { get; init; } = new Dictionary<string, PageComponent>();

    public Component GetComponent(string pageName, string componentId)
    {
        if (!Pages.TryGetValue(pageName, out var page))
        {
            throw new Exception($"Unknown page name {pageName}");
        }

        if (!page.ComponentLookup.TryGetValue(componentId, out var component))
        {
            throw new Exception($"Unknown component {componentId} on {pageName}");
        }
        return component;
    }

    public object? GetComponentData(string componentId, ComponentContext context, IDataModelAccessor dataModel)
    {
        if (context.Component is GroupComponent)
        {
            //TODO before release
            throw new NotImplementedException("Component lookup for components in groups not implemented");
        }

        var component = GetComponent(context.Component.Page, componentId);

        if (!component.DataModelBindings.TryGetValue("simpleBinding", out var binding))
        {
            throw new Exception("component lookup requires the target component ");
        }
        return dataModel.GetModelData(binding);
    }
}


public class PageComponent : Component
{
    public PageComponent(string id, List<Component> children, Dictionary<string, Component> componentLookup ) :
        base(id, "page", null, children, hidden: null, required: null) //TODO: add support for hidden and required on page
    {
        ComponentLookup = componentLookup;
    }

    /// <summary>
    /// Helper dictionary to find components without raversing childern.
    /// </summary>
    public Dictionary<string, Component> ComponentLookup { get;}
}

public class RepeatingGroupComponent : GroupComponent
{
    public RepeatingGroupComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<Component> children, int maxCount, LayoutExpression? hidden, LayoutExpression? required) :
        base(id, type, dataModelBindings, children, hidden, required)
    {
        MaxCount = maxCount;
    }

    public int MaxCount { get; }
}

public class GroupComponent : Component
{
    public GroupComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<Component> children, LayoutExpression? hidden, LayoutExpression? required) :
        base(id, type, dataModelBindings, children, hidden, required)
    {
    }
}

/// <summary>
/// Inteface to be able to handle all component groups the same way.
/// </summary>
public class Component
{
    public Component(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<Component>? children, LayoutExpression? hidden, LayoutExpression? required) 
    {
        Id = id;
        Type = type;
        DataModelBindings = dataModelBindings ?? ImmutableDictionary<string,string>.Empty;
        Hidden = hidden;
        Required = required;
        Children = children ?? Enumerable.Empty<Component>();
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
    public Component? Parent { get; internal set; }

    /// <summary>
    /// The children in this group/page
    /// </summary>
    public IEnumerable<Component> Children { get; internal set; }
}


using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Expressions;

[JsonConverter(typeof(ComponentModelConverter))]
public class ComponentModel
{
    public Dictionary<string, PageComponent> Pages { get; init; } = new Dictionary<string, PageComponent>();

    public BaseComponent GetComponent(string pageName, string componentId)
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

    /// <summary>
    /// Get all components by recursivly walking all the pages.
    /// </summary>
    public IEnumerable<BaseComponent> GetComponents()
    {
        var nodes = new Stack<BaseComponent>(Pages.Values);
        while (nodes.Any())
        {
            var node = nodes.Pop();
            yield return node;
            foreach (var n in node.Children) nodes.Push(n);
        }
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


using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout.Components;

namespace Altinn.App.Core.Models.Layout;

/// <summary>
/// Class for handeling a full layout/layoutset
/// </summary>
public class LayoutModel
{
    /// <summary>
    /// Dictionary to hold the different pages that are part of this LayoutModel
    /// </summary>
    public Dictionary<string, PageComponent> Pages { get; init; } = new Dictionary<string, PageComponent>();

    /// <summary>
    /// Get a page from the <see cref="Pages" /> dictionary
    /// </summary>
    public PageComponent GetPage(string pageName)
    {
        if (Pages.TryGetValue(pageName, out var page))
        {
            return page;
        }
        throw new ArgumentException($"Unknown page name {pageName}");
    }

    /// <summary>
    /// Get a specific component on a specifc page.
    /// </summary>
    public BaseComponent GetComponent(string pageName, string componentId)
    {
        var page = GetPage(pageName);

        if (!page.ComponentLookup.TryGetValue(componentId, out var component))
        {
            throw new ArgumentException($"Unknown component {componentId} on {pageName}");
        }
        return component;
    }

    /// <summary>
    /// Get all components by recursivly walking all the pages.
    /// </summary>
    public IEnumerable<BaseComponent> GetComponents()
    {
        // Use a stack in order to implement a depth first search
        var nodes = new Stack<BaseComponent>(Pages.Values);
        while (nodes.Count != 0)
        {
            var node = nodes.Pop();
            yield return node;
            if (node is GroupComponent groupNode)
                foreach (var n in groupNode.Children)
                    nodes.Push(n);
        }
    }

    /// <summary>
    /// </summary>
    /// <returns>Enumerable </returns>
    public bool HasExternalModelReferences()
    {
        foreach (var component in GetComponents())
        {
            // check external bindings
            foreach (var binding in component.DataModelBindings.Values)
            {
                if (binding.DataType is not null)
                {
                    return true;
                }
            }

            // check expressions
            if (
                HasExternalModelReferences(component.Hidden)
                || HasExternalModelReferences(component.ReadOnly)
                || HasExternalModelReferences(component.Required)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasExternalModelReferences(Expression? expression)
    {
        return IsDataModelExpressionWithTwoAruments(expression)
            || (expression?.Args?.TrueForAll(HasExternalModelReferences) ?? false);
    }

    private static bool IsDataModelExpressionWithTwoAruments(Expression? expression)
    {
        return expression is { Function: ExpressionFunction.dataModel, Args: [_, _] };
    }
}

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Models.Expression;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Component like object to add Page as a group like object
/// </summary>
public class PageComponent : GroupComponent
{
    /// <summary>
    /// Constructor for PageComponent
    /// </summary>
    public PageComponent(string id, List<BaseComponent> children, Dictionary<string, BaseComponent> componentLookup, LayoutExpression? hidden, LayoutExpression? required, LayoutExpression? readOnly, IReadOnlyDictionary<string, string>? extra) :
        base(id, "page", null, children, hidden, required, readOnly, extra)
    {
        ComponentLookup = componentLookup;
    }

    /// <summary>
    /// Helper dictionary to find components without traversing childern.
    /// </summary>
    public Dictionary<string, BaseComponent> ComponentLookup { get; }
}

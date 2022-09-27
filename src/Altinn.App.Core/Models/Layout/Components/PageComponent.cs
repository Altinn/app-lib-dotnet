using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Expressions;

/// <summary>
/// Component like object to add Page as a group like object
/// </summary>
public class PageComponent : BaseComponent
{
    public PageComponent(string id, List<BaseComponent> children, Dictionary<string, BaseComponent> componentLookup) :
        base(id, "page", null, children, hidden: null, required: null) //TODO: add support for hidden and required on page
    {
        ComponentLookup = componentLookup;
    }

    /// <summary>
    /// Helper dictionary to find components without traversing childern.
    /// </summary>
    public Dictionary<string, BaseComponent> ComponentLookup { get; }
}

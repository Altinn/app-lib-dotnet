using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Expressions;

/// <summary>
/// Component specialisation for repeating groups with maxCount > 1
/// </summary>
public class RepeatingGroupComponent : GroupComponent
{
    public RepeatingGroupComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<BaseComponent> children, int maxCount, LayoutExpression? hidden, LayoutExpression? required) :
        base(id, type, dataModelBindings, children, hidden, required)
    {
        MaxCount = maxCount;
    }

    public int MaxCount { get; }
}
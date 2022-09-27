using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Expressions;

public class GroupComponent : BaseComponent
{
    public GroupComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<BaseComponent> children, LayoutExpression? hidden, LayoutExpression? required) :
        base(id, type, dataModelBindings, children, hidden, required)
    {
    }
}
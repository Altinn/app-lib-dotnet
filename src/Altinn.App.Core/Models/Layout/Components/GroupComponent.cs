using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Models.Layout;

namespace Altinn.App.Core.Models.Layout.Components;

public class GroupComponent : BaseComponent
{
    public GroupComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<BaseComponent> children, LayoutExpression? hidden, LayoutExpression? required) :
        base(id, type, dataModelBindings, children, hidden, required)
    {
    }
}
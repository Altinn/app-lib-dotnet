using System.Text.Json;

using Altinn.App.Core.Models.Expression;

namespace Altinn.App.Core.Models.Layout;

/// <summary>
/// Tag component to signify that this is a group component
/// </summary>
public class GroupComponent : BaseComponent
{
    /// <summary>
    /// Constructor for GroupComponent
    /// </summary>
    public GroupComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<BaseComponent> children, LayoutExpression? hidden, LayoutExpression? required, IReadOnlyDictionary<string, JsonElement> extra) :
        base(id, type, dataModelBindings, hidden, required, extra)
    {
        Children = children;
        foreach (var child in Children)
        {
            child.Parent = this;
        }
    }
    
    /// <summary>
    /// The children in this group/page
    /// </summary>
    public IEnumerable<BaseComponent> Children { get; internal set; }
}
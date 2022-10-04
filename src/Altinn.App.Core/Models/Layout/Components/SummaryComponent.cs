using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Models.Expression;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Custom component for handeling the special fields in "type" = "Summary"
/// </summary>
public class SummaryComponent : BaseComponent
{
    /// <summary>
    /// <see cref="BaseComponent.Id" /> of the component this summary references
    /// </summary>
    public string ComponentRef { get; set; }

    /// <summary>
    /// Name of the page this summary component references
    /// </summary>
    public string PageRef { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public SummaryComponent(string id, string type, LayoutExpression? hidden, string componentRef, string pageRef, IReadOnlyDictionary<string, JsonElement> extra) :
        base(id, type, null, hidden, null, extra)
    {
        ComponentRef = componentRef;
        PageRef = pageRef;
    }
}


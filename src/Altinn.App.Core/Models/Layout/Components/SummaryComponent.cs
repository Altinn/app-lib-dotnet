using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Models.Layout;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Custom component for handeling the special fields in "type" = "Summary"
/// </summary>
public class SummaryComponent : BaseComponent
{
    public string ComponentRef { get; set; }
    public string PageRef { get; set; }
    public SummaryComponent(string id, string type, LayoutExpression? hidden, string componentRef, string pageRef) :
        base(id, type, null, null, hidden, null)
    {
        ComponentRef = componentRef;
        PageRef = pageRef;
    }
}


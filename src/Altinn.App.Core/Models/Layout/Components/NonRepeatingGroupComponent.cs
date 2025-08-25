using System.Text.Json;
using Altinn.App.Core.Models.Layout.Components.Base;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// This class represents multiple component types with children as List[string] that are not repeating.
/// </summary>
public sealed class NonRepeatingGroupComponent : SimpleReferenceComponent
{
    /// <summary>
    /// Constructor for NonRepeatingGroupComponent
    /// </summary>
    public NonRepeatingGroupComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId)
    {
        if (!componentElement.TryGetProperty("children", out JsonElement childIdsElement))
        {
            throw new JsonException($"{Type} component must have a \"children\" property.");
        }

        ChildReferences = GetChildrenWithoutMultipageGroupIndex(componentElement, "children");
    }

    /// <inheritdoc />
    public override IReadOnlyCollection<string> ChildReferences { get; }
}

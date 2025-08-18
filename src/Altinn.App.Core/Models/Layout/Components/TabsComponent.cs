using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Models.Layout.Components.Base;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// This class represents multiple component types
/// </summary>
public sealed class TabsComponent : SimpleReferenceComponent
{
    /// <summary>
    /// Constructor for TabsComponent
    /// </summary>
    public TabsComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId)
    {
        if (!componentElement.TryGetProperty("tabs", out JsonElement tabsElement))
        {
            throw new JsonException($"{Type} component must have a \"tabs\" property.");
        }

        Tabs =
            tabsElement.Deserialize<List<TabsConfig>>()
            ?? throw new JsonException("Failed to deserialize tabs in TabsComponent.");

        ChildReferences = Tabs.SelectMany(t => t.Children ?? []).ToList();
    }

    /// <inheritdoc />
    public override IReadOnlyCollection<string> ChildReferences { get; }

    /// <summary>
    /// Configuration for the tabs in the TabsComponent.
    /// </summary>
    public IReadOnlyCollection<TabsConfig> Tabs { get; }
}

/// <summary>
/// Configuration for a single tab in a TabsComponent.
/// </summary>
public sealed class TabsConfig
{
    /// <summary>
    /// List of child component IDs that belong to this tab.
    /// </summary>
    [JsonPropertyName("children")]
    public List<string>? Children { get; init; }
}

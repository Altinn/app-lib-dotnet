using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Models.Layout.Components.Base;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// This class represents a component that references other components and displays them in a card-like format.
/// </summary>
public sealed class CardsComponent : SimpleReferenceComponent
{
    /// <summary>
    /// Constructor for CardsComponent
    /// </summary>
    public CardsComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId)
    {
        if (!componentElement.TryGetProperty("cards", out JsonElement cardsElement))
        {
            throw new JsonException($"{Type} component must have a \"cards\" property.");
        }

        Cards =
            cardsElement.Deserialize<List<CardsConfig>>()
            ?? throw new JsonException("Failed to deserialize tabs in TabsComponent.");

        ChildReferences = Cards.SelectMany(t => t.Children ?? []).ToList();
    }

    /// <inheritdoc />
    public override IReadOnlyCollection<string> ChildReferences { get; }

    /// <summary>
    /// Configuration for the cards in the CardsComponent.
    /// </summary>
    public IReadOnlyCollection<CardsConfig> Cards { get; }
}

/// <summary>
/// Configuration for a single tab in a CardsComponent.
/// </summary>
public sealed class CardsConfig
{
    /// <summary>
    /// List of child component IDs that belong to this tab.
    /// </summary>
    [JsonPropertyName("children")]
    public List<string>? Children { get; init; }
}

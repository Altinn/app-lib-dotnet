using System.Text.Json;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Component specialisation for repeating groups with maxCount > 1
/// </summary>
public sealed class RepeatingGroupComponent : Base.RepeatingReferenceComponent
{
    /// <summary>
    /// Constructor for RepeatingGroupComponent
    /// </summary>
    public RepeatingGroupComponent(JsonElement componentElement, string pageId, string layoutId, int maxCount)
        : base(componentElement, pageId, layoutId)
    {
        MaxCount = maxCount;
        if (
            !componentElement.TryGetProperty("children", out JsonElement childIdsElement)
            || childIdsElement.ValueKind != JsonValueKind.Array
        )
        {
            throw new JsonException($"{Type} must have a \"children\" property that contains a list of strings.");
        }

        if (!DataModelBindings.TryGetValue("group", out var groupBinding))
        {
            throw new JsonException($"{Type} must have a 'group' data model binding.");
        }

        if (string.IsNullOrWhiteSpace(groupBinding.Field))
        {
            throw new JsonException(
                $"Component {layoutId}.{pageId}.{Id} must have 'dataModelBindings.group' which is a non-empty string or object with a non-empty 'field'."
            );
        }

        GroupModelBinding = groupBinding;

        RepeatingChildReferences = GetChildrenWithoutMultipageGroupIndex(componentElement, "children");

        HiddenRow = componentElement.TryGetProperty("hiddenRow", out JsonElement hiddenRowElement)
            ? ExpressionConverter.ReadStatic(hiddenRowElement)
            : Expression.False;
        RowsBefore = ParseGridConfig("rowsBefore", componentElement);

        RowsAfter = ParseGridConfig("rowsAfter", componentElement);

        BeforeChildReferences = RowsBefore
            .SelectMany(row => row.Cells?.Select(cell => cell?.ComponentId) ?? [])
            .OfType<string>()
            .ToList();

        AfterChildReferences = RowsAfter
            .SelectMany(row => row.Cells?.Select(cell => cell?.ComponentId) ?? [])
            .OfType<string>()
            .ToList();
    }

    private static List<GridComponent.GridRowConfig> ParseGridConfig(string propertyName, JsonElement componentElement)
    {
        if (
            componentElement.TryGetProperty(propertyName, out JsonElement gridConfigElement)
            && gridConfigElement.ValueKind != JsonValueKind.Null
        )
        {
            if (gridConfigElement.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException(
                    $"If present, RepeatingGroupComponent \"{propertyName}\" property must be an array."
                );
            }
            return gridConfigElement.Deserialize<List<GridComponent.GridRowConfig>>()
                ?? throw new JsonException($"Failed to deserialize {propertyName} in RepeatingGroupComponent.");
        }

        return [];
    }

    /// <summary>
    /// Maximum number of repetitions of this repeating group
    /// </summary>
    public int MaxCount { get; }

    /// <summary>
    /// List of rows before the repeating group, used to associate components that are not repeated to the repeating group for layout purposes
    /// </summary>
    public IReadOnlyList<GridComponent.GridRowConfig> RowsBefore { get; }

    /// <summary>
    /// List of rows after the repeating group, used to associate components that are not repeated to the repeating group for layout purposes
    /// </summary>
    public IReadOnlyList<GridComponent.GridRowConfig> RowsAfter { get; }

    /// <inheritdoc />
    public override ModelBinding GroupModelBinding { get; }

    /// <inheritdoc />
    public override Expression HiddenRow { get; }

    /// <inheritdoc />
    public override IReadOnlyList<string> RepeatingChildReferences { get; }

    /// <inheritdoc />
    public override IReadOnlyList<string> BeforeChildReferences { get; }

    /// <inheritdoc />
    public override IReadOnlyList<string> AfterChildReferences { get; }
}

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
        GroupModelBinding = groupBinding;

        RepeatingChildReferences = GetChildrenWithoutMultipageGroupIndex(componentElement, "children");

        HiddenRow = componentElement.TryGetProperty("hiddenRow", out JsonElement hiddenRowElement)
            ? ExpressionConverter.ReadStatic(hiddenRowElement)
            : Expression.False;

        if (
            componentElement.TryGetProperty("rowsBefore", out JsonElement rowsBeforeElement)
            && rowsBeforeElement.ValueKind == JsonValueKind.Array
        )
        {
            RowsBefore =
                rowsBeforeElement.Deserialize<List<GridComponent.GridRowConfig>>()
                ?? throw new JsonException("Failed to deserialize rowsBefore in RepeatingGroupComponent.");
        }
        else
        {
            RowsBefore = [];
        }

        if (
            componentElement.TryGetProperty("rowsAfter", out JsonElement rowsAfterElement)
            && rowsAfterElement.ValueKind == JsonValueKind.Array
        )
        {
            RowsAfter =
                rowsAfterElement.Deserialize<List<GridComponent.GridRowConfig>>()
                ?? throw new JsonException("Failed to deserialize rowsAfter in RepeatingGroupComponent.");
        }
        else
        {
            RowsAfter = [];
        }

        NonRepeatingChildReferences = RowsBefore
            .Concat(RowsAfter)
            .SelectMany(row => row.Cells.Select(cell => cell?.ComponentId))
            .OfType<string>()
            .ToList();
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
    public override IReadOnlyList<string> NonRepeatingChildReferences { get; }
}

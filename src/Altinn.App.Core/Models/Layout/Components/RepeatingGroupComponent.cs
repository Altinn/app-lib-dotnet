using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Component specialization for repeating groups with maxCount > 1
/// </summary>
public sealed class RepeatingGroupComponent : Base.RepeatingReferenceComponent
{
    /// <summary>
    /// Parser for RepeatingGroupComponent
    /// </summary>
    public static RepeatingGroupComponent Parse(
        JsonElement componentElement,
        string pageId,
        string layoutId,
        int maxCount
    )
    {
        var id = ParseId(componentElement);
        var type = ParseType(componentElement);
        var dataModelBindings = ParseDataModelBindings(componentElement);

        if (
            !componentElement.TryGetProperty("children", out JsonElement childIdsElement)
            || childIdsElement.ValueKind != JsonValueKind.Array
        )
        {
            throw new JsonException($"{type} must have a \"children\" property that contains a list of strings.");
        }

        if (!dataModelBindings.TryGetValue("group", out var groupModelBinding))
        {
            throw new JsonException($"{type} must have a 'group' data model binding.");
        }

        if (string.IsNullOrWhiteSpace(groupModelBinding.Field))
        {
            throw new JsonException(
                $"Component {layoutId}.{pageId}.{id} must have 'dataModelBindings.group' which is a non-empty string or object with a non-empty 'field'."
            );
        }

        var repeatingChildReferences = ParseChildReferences(componentElement, layoutId, pageId);

        var rowsBefore = ParseGridConfig("rowsBefore", componentElement);

        var rowsAfter = ParseGridConfig("rowsAfter", componentElement);

        var beforeChildReferences = rowsBefore
            .SelectMany(row => row.Cells?.Select(cell => cell?.ComponentId) ?? [])
            .OfType<string>()
            .ToList();

        var afterChildReferences = rowsAfter
            .SelectMany(row => row.Cells?.Select(cell => cell?.ComponentId) ?? [])
            .OfType<string>()
            .ToList();

        return new RepeatingGroupComponent
        {
            Id = id,
            Type = type,
            PageId = pageId,
            LayoutId = layoutId,
            Required = ParseRequiredExpression(componentElement),
            ReadOnly = ParseReadOnlyExpression(componentElement),
            Hidden = ParseHiddenExpression(componentElement),
            RemoveWhenHidden = ParseRemoveWhenHiddenExpression(componentElement),
            DataModelBindings = dataModelBindings,
            TextResourceBindings = ParseTextResourceBindings(componentElement),
            // RepeatingGroupComponent properties
            MaxCount = maxCount,
            GroupModelBinding = groupModelBinding,
            RepeatingChildReferences = repeatingChildReferences,
            HiddenRow = ParseExpression(componentElement, "hiddenRow"),
            RowsBefore = rowsBefore,
            RowsAfter = rowsAfter,
            BeforeChildReferences = beforeChildReferences,
            AfterChildReferences = afterChildReferences,
        };
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
    public required int MaxCount { get; init; }

    /// <summary>
    /// List of rows before the repeating group, used to associate components that are not repeated to the repeating group for layout purposes
    /// </summary>
    public required IReadOnlyList<GridComponent.GridRowConfig> RowsBefore { get; init; }

    /// <summary>
    /// List of rows after the repeating group, used to associate components that are not repeated to the repeating group for layout purposes
    /// </summary>
    public required IReadOnlyList<GridComponent.GridRowConfig> RowsAfter { get; init; }

    /// <inheritdoc />
    public override async Task<ComponentContext> GetContext(
        LayoutEvaluatorState state,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes,
        Dictionary<string, LayoutSetComponent> layoutsLookup
    )
    {
        if (
            ClaimedChildrenLookup is null
            || RepeatingChildReferences is null
            || BeforeChildReferences is null
            || GroupModelBinding.Field is null
            || AfterChildReferences is null
        )
        {
            throw new InvalidOperationException(
                $"{GetType().Name} must call {nameof(ClaimChildren)} before calling {nameof(GetContext)}."
            );
        }

        var childContexts = new List<ComponentContext>();

        foreach (var componentId in BeforeChildReferences)
        {
            childContexts.Add(
                await GetChildContext(componentId, state, defaultDataElementIdentifier, rowIndexes, layoutsLookup)
            );
        }

        var rowCount = await state.GetModelDataCount(GroupModelBinding, defaultDataElementIdentifier, rowIndexes) ?? 0;

        for (int i = 0; i < rowCount; i++)
        {
            var subRowIndexes = GetSubRowIndexes(rowIndexes, i);
            List<ComponentContext> rowChildren = [];
            foreach (var componentId in RepeatingChildReferences)
            {
                rowChildren.Add(
                    await GetChildContext(
                        componentId,
                        state,
                        defaultDataElementIdentifier,
                        subRowIndexes,
                        layoutsLookup
                    )
                );
            }

            childContexts.Add(
                new ComponentContext(
                    state,
                    new RepeatingGroupRowComponent(this, i),
                    subRowIndexes,
                    defaultDataElementIdentifier,
                    rowChildren
                )
            );
        }

        foreach (var componentId in AfterChildReferences)
        {
            childContexts.Add(
                await GetChildContext(componentId, state, defaultDataElementIdentifier, rowIndexes, layoutsLookup)
            );
        }

        return new ComponentContext(state, this, rowIndexes, defaultDataElementIdentifier, childContexts);
    }

    private async Task<ComponentContext> GetChildContext(
        string componentId,
        LayoutEvaluatorState state,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes,
        Dictionary<string, LayoutSetComponent> layoutsLookup
    )
    {
        Debug.Assert(ClaimedChildrenLookup is not null, "Must call ClaimChildren before GetContext");
        if (!ClaimedChildrenLookup.TryGetValue(componentId, out var childComponent))
        {
            throw new ArgumentException($"Child component with id {componentId} not found in claimed children.");
        }

        return await childComponent.GetContext(state, defaultDataElementIdentifier, rowIndexes, layoutsLookup);
    }
}

/// <summary>
/// Component for each row (not read from JSON layout, but created when generating contexts for repeating groups).
/// </summary>
public class RepeatingGroupRowComponent : Base.BaseComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatingGroupRowComponent"/> class from the surrounding group component.
    /// </summary>
    [SetsRequiredMembers]
    public RepeatingGroupRowComponent(RepeatingGroupComponent repeatingReferenceComponent, int index)
    {
        Id = $"{repeatingReferenceComponent.Id}__group_row_{index}";
        PageId = repeatingReferenceComponent.PageId;
        LayoutId = repeatingReferenceComponent.LayoutId;
        DataModelBindings = repeatingReferenceComponent.DataModelBindings;
        RemoveWhenHidden = repeatingReferenceComponent.RemoveWhenHidden;
        Type = "repeatingGroupRow";
        ReadOnly = Expression.False; // We don't have a row level readOnly, only at the group or child component level
        Required = Expression.False; // We don't have a row level required, only at the group or child component level
        TextResourceBindings = repeatingReferenceComponent.TextResourceBindings;
        if (DataModelBindings.TryGetValue("group", out var groupBinding))
        {
            Hidden = repeatingReferenceComponent.HiddenRow;
        }
        else
            Hidden = new Expression(
                ExpressionFunction.or,
                new Expression(
                    ExpressionFunction.dataModel,
                    new Expression(groupBinding.Field),
                    new Expression(groupBinding.DataType)
                ),
                repeatingReferenceComponent.HiddenRow
            );
    }
}

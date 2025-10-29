using System.Text.Json;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Component specialization for Likert components.
/// Likert components are used for survey-style questions where multiple items share the same rating scale.
/// </summary>
public sealed class LikertComponent : Base.RepeatingReferenceComponent
{
    /// <summary>
    /// Parser for LikertComponent
    /// </summary>
    public static LikertComponent Parse(JsonElement componentElement, string pageId, string layoutId)
    {
        var id = ParseId(componentElement);
        var type = ParseType(componentElement);
        var dataModelBindings = ParseDataModelBindings(componentElement);

        // Likert components must have a 'questions' data model binding that points to the repeating collection
        if (!dataModelBindings.TryGetValue("questions", out var questionsModelBinding))
        {
            throw new JsonException($"{type} must have a 'questions' data model binding.");
        }

        if (string.IsNullOrWhiteSpace(questionsModelBinding.Field))
        {
            throw new JsonException(
                $"Component {layoutId}.{pageId}.{id} must have 'dataModelBindings.questions' which is a non-empty string or object with a non-empty 'field'."
            );
        }

        // Likert components are self-contained and don't have external child components
        // The rows are generated internally based on the questions collection
        var repeatingChildReferences = Array.Empty<string>();
        var beforeChildReferences = Array.Empty<string>();
        var afterChildReferences = Array.Empty<string>();

        // Parse optional filter for row indices
        var rowFilter = ParseRowFilter(componentElement);

        return new LikertComponent
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
            GroupModelBinding = questionsModelBinding,
            RepeatingChildReferences = repeatingChildReferences,
            HiddenRow = Expression.False,
            BeforeChildReferences = beforeChildReferences,
            AfterChildReferences = afterChildReferences,
            RowFilter = rowFilter,
        };
    }

    private static RowFilter? ParseRowFilter(JsonElement componentElement)
    {
        if (!componentElement.TryGetProperty("filter", out var filterElement))
        {
            return null;
        }

        int? start = null;
        int? stop = null;

        if (filterElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in filterElement.EnumerateArray())
            {
                if (
                    item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("key", out var keyProp)
                    && item.TryGetProperty("value", out var valueProp)
                )
                {
                    var key = keyProp.GetString();
                    if (key == "start" && valueProp.ValueKind == JsonValueKind.String)
                    {
                        if (int.TryParse(valueProp.GetString(), out var startValue))
                        {
                            start = startValue;
                        }
                    }
                    else if (key == "stop" && valueProp.ValueKind == JsonValueKind.String)
                    {
                        if (int.TryParse(valueProp.GetString(), out var stopValue))
                        {
                            stop = stopValue;
                        }
                    }
                }
            }
        }

        if (start.HasValue && stop.HasValue)
        {
            return new RowFilter { Start = start.Value, Stop = stop.Value };
        }

        return null;
    }
}

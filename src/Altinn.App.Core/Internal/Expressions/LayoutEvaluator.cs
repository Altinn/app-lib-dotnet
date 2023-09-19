using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Internal.Expressions;

/// <summary>
/// Utilities for using the expression results to do tasks in backend
/// </summary>
public static class LayoutEvaluator
{
    /// <summary>
    /// Get a list of fields that are only referenced in hidden components in <see cref="LayoutEvaluatorState" />
    /// </summary>
    public static List<string> GetHiddenFieldsForRemoval(LayoutEvaluatorState state)
    {
        var hiddenModelBindings = new HashSet<string>();
        var nonHiddenModelBindings = new HashSet<string>();

        foreach (var context in state.GetComponentContexts())
        {
            HiddenFieldsForRemovalRecurs(state, hiddenModelBindings, nonHiddenModelBindings, context, parentHidden: false);
        }

        var forRemoval = hiddenModelBindings.Except(nonHiddenModelBindings);
        var existsForRemoval = forRemoval.Where(key => state.GetModelData(key) is not null);
        return existsForRemoval.ToList();
    }

    private static void HiddenFieldsForRemovalRecurs(LayoutEvaluatorState state, HashSet<string> hiddenModelBindings, HashSet<string> nonHiddenModelBindings, ComponentContext context, bool parentHidden)
    {
        var hidden = parentHidden || ExpressionEvaluator.EvaluateBooleanExpression(state, context, "hidden", false);

        // Hidden row for repeating group
        var hiddenRow = new Dictionary<int, bool>();
        if (context.Component is RepeatingGroupComponent repGroup && context.RowLength is not null && repGroup.HiddenRow is not null)
        {
            foreach (var index in Enumerable.Range(0, context.RowLength.Value).Reverse())
            {
                var rowIndices = context.RowIndices?.Append(index).ToArray() ?? new[] { index };
                var childContexts = context.ChildContexts.Where(c => c.RowIndices?.Last() == index);
                var rowContext = new ComponentContext(context.Component, rowIndices, null, childContexts);
                var rowHidden = ExpressionEvaluator.EvaluateBooleanExpression(state, rowContext, "hiddenRow", false);
                hiddenRow.Add(index, rowHidden);

                var indexedBinding = state.AddInidicies(repGroup.DataModelBindings["group"], rowContext);
                if (rowHidden)
                {
                    hiddenModelBindings.Add(indexedBinding);
                }
                else
                {
                    nonHiddenModelBindings.Add(indexedBinding);
                }
            }
        }

        foreach (var childContext in context.ChildContexts)
        {
            // Check if row is already hidden
            if (context.Component is RepeatingGroupComponent)
            {
                var currentRow = childContext.RowIndices?.Last();
                var rowIsHidden = currentRow is not null && hiddenRow.GetValueOrDefault(currentRow.Value);
                if (rowIsHidden)
                {
                    continue;
                }
            }

            HiddenFieldsForRemovalRecurs(state, hiddenModelBindings, nonHiddenModelBindings, childContext, hidden);
        }

        foreach (var (bindingName, binding) in context.Component.DataModelBindings)
        {
            if (bindingName == "group")
            {
                continue;
            }

            var indexed_binding = state.AddInidicies(binding, context);

            if (hidden)
            {
                hiddenModelBindings.Add(indexed_binding);
            }
            else
            {
                nonHiddenModelBindings.Add(indexed_binding);
            }
        }
    }

    /// <summary>
    /// Remove fields that are only refrenced from hidden fields from the data object in the state.
    /// </summary>
    public static void RemoveHiddenData(LayoutEvaluatorState state, bool deleteRows = false)
    {
        var fields = GetHiddenFieldsForRemoval(state);
        foreach (var field in fields)
        {
            state.RemoveDataField(field, deleteRows);
        }
    }

    /// <summary>
    /// Return a list of <see cref="ValidationIssue" /> for the given state and dataElementId
    /// </summary>
    public static IEnumerable<ValidationIssue> RunLayoutValidationsForRequired(LayoutEvaluatorState state, string dataElementId)
    {
        var validationIssues = new List<ValidationIssue>();

        foreach (var context in state.GetComponentContexts())
        {
            RunLayoutValidationsForRequiredRecurs(validationIssues, state, dataElementId, context);
        }

        return validationIssues;
    }

    private static void RunLayoutValidationsForRequiredRecurs(List<ValidationIssue> validationIssues, LayoutEvaluatorState state, string dataElementId, ComponentContext context)
    {
        var hidden = ExpressionEvaluator.EvaluateBooleanExpression(state, context, "hidden", false);
        if (!hidden)
        {
            foreach (var childContext in context.ChildContexts)
            {
                RunLayoutValidationsForRequiredRecurs(validationIssues, state, dataElementId, childContext);
            }

            var required = ExpressionEvaluator.EvaluateBooleanExpression(state, context, "required", false);
            if (required)
            {
                foreach (var (bindingName, binding) in context.Component.DataModelBindings)
                {
                    if (state.GetModelData(binding, context) is null)
                    {
                        var field = state.AddInidicies(binding, context);
                        validationIssues.Add(new ValidationIssue()
                        {
                            Severity = ValidationIssueSeverity.Error,
                            InstanceId = state.GetInstanceContext("instanceId").ToString(),
                            DataElementId = dataElementId,
                            Field = field,
                            Description = $"{field} is required in component with id {context.Component.Id}",
                            Code = "required",
                            Source = ValidationIssueSources.Required
                        });
                    }
                }
            }
        }
    }
}

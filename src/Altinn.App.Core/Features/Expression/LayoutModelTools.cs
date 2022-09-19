using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Features.Expression;

/// <summary>
/// Utilities for using the expression results to do tasks in backend
/// </summary>
public static class LayoutModelTools
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
            var hidden = ExpressionEvaluator.EvaluateBooleanExpression(state, context.Component, "hidden", false, context);
            var dataModelBindings = LayoutEvaluatorState.GetModelBindings(context.Component.Element);
            if (dataModelBindings?.Count > 0)
            {
                foreach (var binding in dataModelBindings.Values)
                {
                    var indexed_binding = state.AddInidicies(binding, context);
                    if (indexed_binding is not null)
                    {
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
            }
        }

        var forRemoval = hiddenModelBindings.Except(nonHiddenModelBindings);
        var existsForRemoval = forRemoval.Where(key => state.GetModelData(key) is not null);
        return existsForRemoval.ToList();
    }

    /// <summary>
    /// Remove fields that are only refrenced from hidden fields from the data object in the state.
    /// </summary>
    public static void RemoveHiddenData(LayoutEvaluatorState state)
    {
        var fields = GetHiddenFieldsForRemoval(state);
        foreach (var field in fields)
        {
            state.RemoveDataField(field);
        }
    }

    /// <summary>
    /// Return a list of <see cref="ValidationIssue" /> for the given state and dataElementId
    /// </summary>
    public static IEnumerable<ValidationIssue> RunLayoutValidationsForRequired(LayoutEvaluatorState state, string dataElementId)
    {
        var ret = new List<ValidationIssue>();

        foreach (var context in state.GetComponentContexts())
        {
            var required = ExpressionEvaluator.EvaluateBooleanExpression(state, context.Component, "required", false, context);
            var hidden = ExpressionEvaluator.EvaluateBooleanExpression(state, context.Component, "hidden", false, context);
            var dataModelBindings = LayoutEvaluatorState.GetModelBindings(context.Component.Element);
            if (required && !hidden && dataModelBindings is not null)
            {
                foreach (var (bindingName, binding) in dataModelBindings)
                {
                    var indexedBinding = state.AddInidicies(binding, context);
                    if (state.GetModelData(binding, context) is null)
                    {
                        ret.Add(new ValidationIssue()
                        {
                            Severity = ValidationIssueSeverity.Error,
                            InstanceId = state.GetInstanceContext("instanceId").ToString(),
                            DataElementId = dataElementId,
                            Field = state.AddInidicies(binding, context),
                            Description = "TODO required",
                            Code = "required",
                        });
                    }
                }
            }
        }

        return ret;
    }
}
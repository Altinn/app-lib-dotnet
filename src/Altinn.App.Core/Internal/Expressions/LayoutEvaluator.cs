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
            HiddenFieldsForRemovalReucrs(state, hiddenModelBindings, nonHiddenModelBindings, context, parentHidden: false);
        }

        var forRemoval = hiddenModelBindings.Except(nonHiddenModelBindings);
        var existsForRemoval = forRemoval.Where(key => state.GetModelData(key) is not null);
        return existsForRemoval.ToList();
    }

    private static void HiddenFieldsForRemovalReucrs(LayoutEvaluatorState state, HashSet<string> hiddenModelBindings, HashSet<string> nonHiddenModelBindings, ComponentContext context, bool parentHidden)
    {
        var hidden = parentHidden || ExpressionEvaluator.EvaluateBooleanExpression(state, context, "hidden", false);

        foreach (var childContext in context.ChildContexts)
        {
            HiddenFieldsForRemovalReucrs(state, hiddenModelBindings, nonHiddenModelBindings, childContext, hidden);
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
                        });
                    }
                }
            }
        }
    }
    public static IEnumerable<ValidationIssue> RunValidationForOptions(LayoutEvaluatorState state, string dataElementId)
    {
        var validationIssues = new List<ValidationIssue>();

        foreach (var context in state.GetComponentContexts())
        {
            RunLayoutValidationsForOptionsRecurs(validationIssues, state, dataElementId, context);
        }

        return validationIssues;
    }

    private static void RunLayoutValidationsForOptionsRecurs(List<ValidationIssue> validationIssues, LayoutEvaluatorState state, string dataElementId, ComponentContext context)
    {
        var hidden = ExpressionEvaluator.EvaluateBooleanExpression(state, context, "hidden", false);
        if (!hidden)
        {
            if (context.Component is OptionsComponent optionsComponent)
            {
                foreach (var value in optionsComponent.GetSelectedValues(state, context))
                {
                    if(!IsValidOption(optionsComponent, value))
                    {
                        validationIssues.Add(new()
                        {
                            Severity = ValidationIssueSeverity.Error,
                            Code = "invalidOption",
                            DataElementId = dataElementId,
                            InstanceId = state.GetInstanceContext("instanceId").ToString(),
                            Field = state.AddInidicies(optionsComponent.DataModelBindings.Values.First(), context),
                            Description = $"{value} is not a valid option",
                        });
                    }
                }
            }
            foreach (var childContext in context.ChildContexts)
            {
                RunLayoutValidationsForRequiredRecurs(validationIssues, state, dataElementId, childContext);
            }
        }
    }

    private static bool IsValidOption(OptionsComponent optionsComponent, string value)
    {
        if (optionsComponent.Options is not null)
        {
            if (!optionsComponent.Options.Any(o => o.Value == value))
            {
                return false;
            }
        }
        else if (optionsComponent.OptionId is not null)
        {
            if(se)
            // TODO: 
        }
        else if (optionsComponent.OptionsSource is not null)
        {
            // TODO: implement validation for options from a "source" in a repeating group
        }
        return true;
    }
}
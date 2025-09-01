using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout;
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
    public static async Task<List<DataReference>> GetHiddenFieldsForRemoval(LayoutEvaluatorState state)
    {
        var hiddenModelBindings = new HashSet<DataReference>();
        var nonHiddenModelBindings = new HashSet<DataReference>();

        var pageContexts = await state.GetComponentContexts();
        foreach (var pageContext in pageContexts)
        {
            await HiddenFieldsForRemovalRecurs(state, hiddenModelBindings, nonHiddenModelBindings, pageContext, []);
        }

        var forRemoval = hiddenModelBindings.Except(nonHiddenModelBindings).ToList();

        return forRemoval;
    }

    private static async Task HiddenFieldsForRemovalRecurs(
        LayoutEvaluatorState state,
        HashSet<DataReference> hiddenModelBindings,
        HashSet<DataReference> nonHiddenModelBindings,
        ComponentContext context,
        IReadOnlyList<DataReference> ignoredPreifxes
    )
    {
        if (context.Component is null)
        {
            throw new ArgumentNullException(
                nameof(context),
                "Context must have a component when removing hidden fields"
            );
        }

        var isHidden = await context.IsHidden();

        List<DataReference> childIgnoredPrefixes = [.. ignoredPreifxes];

        // Schedule fields for removal
        foreach (var (_, binding) in context.Component.DataModelBindings)
        {
            var indexedBinding = await state.AddInidicies(binding, context);
            if (ignoredPreifxes.Any(prefix => indexedBinding.StartsWith(prefix)))
            {
                continue; // Skip fields with ignored prefixes
            }

            if (isHidden)
            {
                hiddenModelBindings.Add(indexedBinding);
                childIgnoredPrefixes.Add(indexedBinding);
            }
            else
            {
                nonHiddenModelBindings.Add(indexedBinding);
            }
        }

        // Recurse children
        foreach (var childContext in context.ChildContexts)
        {
            await HiddenFieldsForRemovalRecurs(
                state,
                hiddenModelBindings,
                nonHiddenModelBindings,
                childContext,
                childIgnoredPrefixes
            );
        }
    }

    /// <summary>
    /// Remove fields that are only referenced from hidden fields from the data object in the state.
    /// </summary>
    [Obsolete("Use the async version of this method RemoveHiddenDataAsync")]
    public static void RemoveHiddenData(LayoutEvaluatorState state, RowRemovalOption rowRemovalOption)
    {
        RemoveHiddenDataAsync(state, rowRemovalOption).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Remove fields that are only referenced from hidden fields from the data object in the state.
    /// </summary>
    public static async Task RemoveHiddenDataAsync(LayoutEvaluatorState state, RowRemovalOption rowRemovalOption)
    {
        var fields = await GetHiddenFieldsForRemoval(state);
        foreach (var dataReference in fields)
        {
            await state.RemoveDataField(dataReference, rowRemovalOption);
        }
    }

    /// <summary>
    /// Return a list of <see cref="ValidationIssue" /> for the given state and dataElementId
    /// </summary>
    public static async Task<List<ValidationIssue>> RunLayoutValidationsForRequired(LayoutEvaluatorState state)
    {
        var validationIssues = new List<ValidationIssue>();

        foreach (var context in await state.GetComponentContexts())
        {
            await RunLayoutValidationsForRequiredRecurs(validationIssues, state, context);
        }

        return validationIssues;
    }

    private static async Task RunLayoutValidationsForRequiredRecurs(
        List<ValidationIssue> validationIssues,
        LayoutEvaluatorState state,
        ComponentContext context
    )
    {
        ArgumentNullException.ThrowIfNull(context.Component);
        var hidden = await context.IsHidden();
        if (!hidden)
        {
            foreach (var childContext in context.ChildContexts)
            {
                await RunLayoutValidationsForRequiredRecurs(validationIssues, state, childContext);
            }

            var required = await ExpressionEvaluator.EvaluateBooleanExpression(state, context, "required", false);
            if (required)
            {
                foreach (var (bindingName, binding) in context.Component.DataModelBindings)
                {
                    var value = await state.GetModelData(binding, context.DataElementIdentifier, context.RowIndices);
                    if (
                        (value is null)
                        || (value is string s && string.IsNullOrWhiteSpace(s))
                        || (value is System.Collections.ICollection col && col.Count == 0)
                    )
                    {
                        var field = await state.AddInidicies(binding, context);
                        validationIssues.Add(
                            new ValidationIssue()
                            {
                                Severity = ValidationIssueSeverity.Error,
                                DataElementId = field.DataElementIdentifier.ToString(),
                                Field = field.Field,
                                Description =
                                    $"{field.Field} is required in component with id {context.Component.LayoutId}.{context.Component.PageId}.{context.Component.Id} for binding {bindingName}",
                                Code = "required",
                            }
                        );
                    }
                }
            }
        }
    }
}

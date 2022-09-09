using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Implementation.Expression;

public class LayoutModelTools
{
    private readonly LayoutEvaluatorStateInitializer _layoutInitializer;

    public LayoutModelTools(LayoutEvaluatorStateInitializer layoutInitializer)
    {
        _layoutInitializer = layoutInitializer;
    }

    public async Task<List<string>> GetHiddenFieldsForRemoval(Guid instanceGuid, Type type, string org, string app, int instanceOwnerPartyId, Guid dataId)
    {
        var state = await _layoutInitializer.Init(instanceGuid, type, org, app, instanceOwnerPartyId, dataId);

        var hiddenModelBindings = new HashSet<string>();
        var nonHiddenModelBindings = new HashSet<string>();

        foreach (var context in state.GetComponentContexts())
        {
            // var context = new ComponentContext{ ComponentId = component.Id, CurrentPageName = component.Page, RowIndices = new List<int>()};
            var hidden = ExpressionEvaluator.EvaluateBooleanExpression(state, context.Component, "hidden", false, context);
            var dataModelBindings = LayoutEvaluatorState.GetModelBindings(context.Component.Element);
            if (dataModelBindings?.Count > 0)
            {
                foreach (var binding in dataModelBindings.Values)
                {
                    if (hidden)
                    {
                        hiddenModelBindings.Add(binding);
                    }
                    else
                    {
                        nonHiddenModelBindings.Add(binding);
                    }
                }
            }
        }

        var forRemoval = hiddenModelBindings.Except(nonHiddenModelBindings);
        var existsForRemoval = forRemoval.Where(key => state.GetModelData(key) is not null);
        return existsForRemoval.ToList();
    }

    public async Task<IEnumerable<ValidationIssue>> RunLayoutValidationsForRequired(Guid instanceGuid, Type type, string org, string app, int instanceOwnerPartyId, Guid dataId)
    {
        var ret = new List<ValidationIssue>();
        var state = await _layoutInitializer.Init(instanceGuid, type, org, app, instanceOwnerPartyId, dataId);

        foreach (var context in state.GetComponentContexts())
        {
            var required = ExpressionEvaluator.EvaluateBooleanExpression(state, context.Component, "required", false, context);
            var hidden = ExpressionEvaluator.EvaluateBooleanExpression(state, context.Component, "hidden", false, context);
            var dataModelBindings = LayoutEvaluatorState.GetModelBindings(context.Component.Element);
            if (required && !hidden && dataModelBindings is not null)
            {
                foreach (var (bindingName, binding) in dataModelBindings)
                {
                    if (state.GetModelData(binding) is null)
                    {
                        ret.Add(new ValidationIssue()
                        {
                            Severity = ValidationIssueSeverity.Error,
                            InstanceId = instanceGuid.ToString(),
                            DataElementId = dataId.ToString(),
                            Field = binding,
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
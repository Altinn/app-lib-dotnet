using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Core.Internal.Expressions;

public class LayoutValidator
{
    private readonly InstanceAppOptionsFactory _instanceAppOptions;
    private readonly AppOptionsFactory _appOptions;

    public LayoutValidator(InstanceAppOptionsFactory instanceAppOptions, AppOptionsFactory appOptions)
    {
        _instanceAppOptions = instanceAppOptions;
        _appOptions = appOptions;
    }

    public async Task<List<ValidationIssue>> Validate(LayoutEvaluatorState state, string dataElementId)
    {
        var validationIssues = new List<ValidationIssue>();

        foreach (var context in state.GetComponentContexts())
        {
            await RunValidationRecurs(validationIssues, state, dataElementId, context);
        }

        return validationIssues;

    }

    private async Task RunValidationRecurs(List<ValidationIssue> validationIssues, LayoutEvaluatorState state, string dataElementId, ComponentContext context)
    {
        var hidden = ExpressionEvaluator.EvaluateBooleanExpression(state, context, "hidden", false);
        if (!hidden)
        {
            foreach (var childContext in context.ChildContexts)
            {
                await RunValidationRecurs(validationIssues, state, dataElementId, childContext);
            }

            var values = context.Component.DataModelBindings.Select(
                b => (bindingName: b.Key, field: state.AddInidicies(b.Value, context), state.GetModelData(b.Value, context))
            ).ToArray();

            // Validate required
            if (ExpressionEvaluator.EvaluateBooleanExpression(state, context, "required", false))
            {
                foreach (var (bindingName, field, value) in values)
                {
                    if (value is null)
                    {
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
            // Validate that option components have valid options
            else if (context.Component is OptionsComponent optionsComponent)
            {
                if(!await IsValidOption(optionsComponent, state, values))
                {
                    validationIssues.Add(new ()
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
    private async Task<bool> IsValidOption(OptionsComponent optionsComponent, LayoutEvaluatorState state, (string bindingName, string field, object?)[] value)
    {
        if (value.Length != 1)
        {
            return true; // we don't have multiple data bindings on options components (yet) 
        }

        if (optionsComponent.Options is not null)
        {
            if (!optionsComponent.Options.Any(o => o.Value == value))
            {
                return false;
            }
        }
        else if (optionsComponent.OptionId is not null)
        {
            if(optionsComponent.Secure)
            {
                var secureOptionsProvider = _instanceAppOptions.GetOptionsProvider(optionsComponent.OptionId);
                optionsComponent.
                var secureOptions = secureOptionsProvider.GetInstanceAppOptionsAsync(new Models.InstanceIdentifier(state.GetInstance()), "nb", keyValuePairs);
            }
        }
        else if (optionsComponent.OptionsSource is not null)
        {
            // TODO: implement validation for options from a "source" in a repeating group
        }
        return true;
    }
}
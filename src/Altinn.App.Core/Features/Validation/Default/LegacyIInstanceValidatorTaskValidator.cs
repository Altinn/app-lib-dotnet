#pragma warning disable CS0618 // Type or member is obsolete
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Validation.Helpers;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Ensures that the old <see cref="IInstanceValidator.ValidateTask(Instance, string, ModelStateDictionary)"/> extention hook is still supported.
/// </summary>
public class LegacyIInstanceValidatorTaskValidator : ITaskValidator
{
    private readonly IInstanceValidator? _instanceValidator;
    private readonly GeneralSettings _generalSettings;

    /// <summary>
    /// Constructor
    /// </summary>
    public LegacyIInstanceValidatorTaskValidator(IInstanceValidator? instanceValidator, IOptions<GeneralSettings> generalSettings)
    {
        _instanceValidator = instanceValidator;
        _generalSettings = generalSettings.Value;
    }

    /// <summary>
    /// Run the legacy validator for all tasks
    /// </summary>
    public string TaskId => "*";

    /// <inheritdoc />
    public async Task<List<ValidationIssue>> ValidateTask(Instance instance, string taskId)
    {
        if (_instanceValidator is null)
        {
            return new List<ValidationIssue>();
        }

        var modelState = new ModelStateDictionary();
        await _instanceValidator.ValidateTask(instance, taskId, modelState);
        return ModelStateHelpers.MapModelStateToIssueList(modelState, instance, _generalSettings);
    }
}
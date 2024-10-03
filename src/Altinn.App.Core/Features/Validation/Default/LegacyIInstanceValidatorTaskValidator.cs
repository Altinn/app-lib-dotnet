#pragma warning disable CS0618 // Type or member is obsolete
using System.Diagnostics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Validation.Helpers;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Ensures that the old <see cref="IInstanceValidator.ValidateTask(Instance, string, ModelStateDictionary)"/> extension hook is still supported.
/// </summary>
public class LegacyIInstanceValidatorTaskValidator : ITaskValidator
{
    private readonly GeneralSettings _generalSettings;
    private readonly AppImplementationFactory _appImplementationFactory;

    private IInstanceValidator? _instanceValidator => _appImplementationFactory.Get<IInstanceValidator>();

    /// <summary>
    /// Constructor
    /// </summary>
    public LegacyIInstanceValidatorTaskValidator(
        IOptions<GeneralSettings> generalSettings,
        IServiceProvider serviceProvider
    )
    {
        _generalSettings = generalSettings.Value;
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
    }

    /// <summary>
    /// Run the legacy validator for all tasks
    /// </summary>
    public string TaskId => "*";

    /// <inheritdoc />
    public string ValidationSource
    {
        get
        {
            var type = _instanceValidator?.GetType() ?? GetType();
            Debug.Assert(type.FullName is not null, "FullName does not return null on class/struct types");
            return type.FullName;
        }
    }

    /// <inheritdoc />
    public async Task<List<ValidationIssue>> ValidateTask(Instance instance, string taskId, string? language)
    {
        var instanceValidator = _instanceValidator;
        if (instanceValidator is null)
        {
            return [];
        }

        var modelState = new ModelStateDictionary();
        await instanceValidator.ValidateTask(instance, taskId, modelState);
        return ModelStateHelpers.MapModelStateToIssueList(modelState, instance, _generalSettings);
    }
}

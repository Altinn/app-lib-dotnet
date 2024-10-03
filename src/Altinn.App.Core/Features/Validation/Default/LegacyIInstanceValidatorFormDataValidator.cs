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
/// This validator is used to run the legacy IInstanceValidator.ValidateData method
/// </summary>
public class LegacyIInstanceValidatorFormDataValidator : IFormDataValidator
{
    private readonly GeneralSettings _generalSettings;
    private readonly AppImplementationFactory _appImplementationFactory;

    private IInstanceValidator? _instanceValidator => _appImplementationFactory.Get<IInstanceValidator>();

    /// <summary>
    /// constructor
    /// </summary>
    public LegacyIInstanceValidatorFormDataValidator(
        IOptions<GeneralSettings> generalSettings,
        IServiceProvider serviceProvider
    )
    {
        _generalSettings = generalSettings.Value;
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
    }

    /// <summary>
    /// The legacy validator should run for all data types
    /// </summary>
    public string DataType => _instanceValidator is null ? "" : "*";

    /// <inheritdoc />>
    public string ValidationSource
    {
        get
        {
            var type = _instanceValidator?.GetType() ?? GetType();
            Debug.Assert(type.FullName is not null, "FullName does not return null on class/struct types");
            return type.FullName;
        }
    }

    /// <summary>
    /// Always run for incremental validation (if it exists)
    /// </summary>
    public bool HasRelevantChanges(object current, object previous) => _instanceValidator is not null;

    /// <inheritdoc />
    public async Task<List<ValidationIssue>> ValidateFormData(
        Instance instance,
        DataElement dataElement,
        object data,
        string? language
    )
    {
        var instanceValidator = _instanceValidator;
        if (instanceValidator is null)
        {
            return [];
        }

        var modelState = new ModelStateDictionary();
        await instanceValidator.ValidateData(data, modelState);
        return ModelStateHelpers.ModelStateToIssueList(
            modelState,
            instance,
            dataElement,
            _generalSettings,
            data.GetType(),
            ValidationIssueSources.Custom
        );
    }
}

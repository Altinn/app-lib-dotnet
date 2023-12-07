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
///
/// </summary>
public class LegacyIValidationFormDataValidator : IFormDataValidator
{
    private readonly IInstanceValidator? _instanceValidator;
    private readonly GeneralSettings _generalSettings;

    /// <summary>
    /// constructor
    /// </summary>
    public LegacyIValidationFormDataValidator(IInstanceValidator? instanceValidator, IOptions<GeneralSettings> generalSettings)
    {
        _instanceValidator = instanceValidator;
        _generalSettings = generalSettings.Value;
    }

    /// <summary>
    ///
    /// </summary>
    public string DataType { get; } = "AnyType";

    /// <summary>
    /// Always run for incremental validation
    /// </summary>
    public bool ShouldRunForIncrementalValidation(List<string>? changedFields = null) => true;


    /// <inheritdoc />
    public async Task<List<ValidationIssue>> ValidateFormData(Instance instance, DataElement dataElement, object data, List<string>? changedFields = null)
    {
        if (_instanceValidator is null)
        {
            return new List<ValidationIssue>();
        }

        var modelState = new ModelStateDictionary();
        await _instanceValidator.ValidateData(data, modelState);
        return ModelStateHelpers.ModelStateToIssueList(modelState, instance, dataElement, _generalSettings, data.GetType(), ValidationIssueSources.Custom);
    }
}
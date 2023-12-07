using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Validation.Helpers;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Runs <see cref="System.ComponentModel.DataAnnotations"/> validation on the data object.
/// </summary>
public class DataAnnotationValidator : IFormDataValidator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IObjectModelValidator _objectModelValidator;
    private readonly GeneralSettings _generalSettings;

    /// <summary>
    /// Constructor
    /// </summary>
    public DataAnnotationValidator([ServiceKey] string dataType, IHttpContextAccessor httpContextAccessor, IObjectModelValidator objectModelValidator, IOptions<GeneralSettings> generalSettings)
    {
        DataType = dataType;
        _httpContextAccessor = httpContextAccessor;
        _objectModelValidator = objectModelValidator;
        _generalSettings = generalSettings.Value;
    }

    /// <summary>
    /// Dummy implementation to satisfy interface, We use <see cref="CanValidateDataType" /> instead
    /// </summary>
    public string DataType { get; }

    /// <summary>
    /// Run validator for all data types.
    /// </summary>
    public bool CanValidateDataType(string dataType) => true;

    /// <summary>
    /// Disable incremental validation for this validator.
    /// </summary>
    public bool ShouldRunForIncrementalValidation(List<string>? changedFields = null) => false;

    /// <inheritdoc />
    public Task<List<ValidationIssue>> ValidateFormData(Instance instance, DataElement dataElement, object data, List<string>? changedFields = null)
    {
        try
        {
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(
                _httpContextAccessor.HttpContext!,
                new Microsoft.AspNetCore.Routing.RouteData(),
                new ActionDescriptor(),
                modelState);
            ValidationStateDictionary validationState = new ValidationStateDictionary();
            _objectModelValidator.Validate(actionContext, validationState, null!, data);

            return Task.FromResult(ModelStateHelpers.ModelStateToIssueList(modelState, instance, dataElement, _generalSettings, data.GetType(), ValidationIssueSources.ModelState));
        }
        catch (Exception e)
        {
            return Task.FromException<List<ValidationIssue>>(e);
        }
    }
}
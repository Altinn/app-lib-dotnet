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
    public DataAnnotationValidator(IHttpContextAccessor httpContextAccessor, IObjectModelValidator objectModelValidator, IOptions<GeneralSettings> generalSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _objectModelValidator = objectModelValidator;
        _generalSettings = generalSettings.Value;
    }

    /// <summary>
    /// Run Data annotation validation on all data types with app logic
    /// </summary>
    public string DataType => "*";

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
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features.Validation.Default;

public class RequiredLayoutValidator : IFormDataValidator
{
    private readonly LayoutEvaluatorStateInitializer _layoutEvaluatorStateInitializer;
    private readonly IAppResources _appResourcesService;
    private readonly IAppMetadata _appMetadata;

    public RequiredLayoutValidator([ServiceKey] string dataType, LayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer, IAppResources appResourcesService, IAppMetadata appMetadata)
    {
        DataType = dataType;
        _layoutEvaluatorStateInitializer = layoutEvaluatorStateInitializer;
        _appResourcesService = appResourcesService;
        _appMetadata = appMetadata;
    }
    /// <inheritdoc />
    public string DataType { get; }

    /// <summary>
    /// Required validator should always run for incremental validation, as they're almost quicker to run than to verify.
    /// </summary>
    public bool ShouldRunForIncrementalValidation(List<string>? changedFields = null) => true;

    /// <summary>
    /// Validate the form data against the required rules in the layout
    /// </summary>
    public async Task<List<ValidationIssue>> ValidateFormData(Instance instance, DataElement dataElement, object data, List<string>? changedFields = null)
    {
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var layoutSet = _appResourcesService.GetLayoutSetForTask(appMetadata.DataTypes.First(dt=>dt.Id == dataElement.DataType).TaskId);
        var evaluationState = await _layoutEvaluatorStateInitializer.Init(instance, data, layoutSet?.Id);
        return LayoutEvaluator.RunLayoutValidationsForRequired(evaluationState, dataElement.Id);
    }
}
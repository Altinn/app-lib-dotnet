using System.Diagnostics;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Validator that runs the required rules in the layout
/// </summary>
public class RequiredLayoutValidator : IFormDataValidator
{
    private readonly LayoutEvaluatorStateInitializer _layoutEvaluatorStateInitializer;
    private readonly IAppResources _appResources;
    private readonly IAppMetadata _appMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredLayoutValidator"/> class.
    /// </summary>
    public RequiredLayoutValidator(
        LayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer,
        IAppResources appResources,
        IAppMetadata appMetadata
    )
    {
        _layoutEvaluatorStateInitializer = layoutEvaluatorStateInitializer;
        _appResources = appResources;
        _appMetadata = appMetadata;
    }

    /// <summary>
    /// Run for all data types
    /// </summary>
    public string DataType => "*";

    /// <summary>
    /// This validator has the code "Required" and this is known by the frontend, who may request this validator to not run for incremental validation.
    /// </summary>
    public string ValidationSource => "Required";

    private static DataType? DefaultDataType(string taskId, ApplicationMetadata appMetadata, LayoutSet? layoutSet)
    {
        // First look for the layoutSet.DataType, then look for the first DataType with a classRef
        return appMetadata.DataTypes.FirstOrDefault(d => layoutSet?.DataType == d.Id)
            ?? appMetadata.DataTypes.FirstOrDefault(d => d.AppLogic?.ClassRef is not null && d.TaskId == taskId);
    }

    /// <summary>
    /// We don't have an efficient way to figure out if changes to the model results in different validations, and frontend ignores this anyway
    /// </summary>
    public bool HasRelevantChanges(object current, object previous) => true;

    /// <inheritdoc />
    public async Task<List<ValidationIssue>> ValidateFormData(
        Instance instance,
        DataElement dataElement,
        object data,
        string? language
    )
    {
        var taskId = instance.Process.CurrentTask.ElementId;

        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var layoutSet = _appResources.GetLayoutSetForTask(taskId);
        var layouts = _appResources.GetLayoutModel(layoutSet?.Id);
        var referencedDataTypes = GetReferencedDataTypes(appMetadata, layoutSet, layouts, instance, taskId);

        var evaluationState = await _layoutEvaluatorStateInitializer.Init(instance, data, layoutSet?.Id);
        var defaultDataType = DefaultDataType(taskId, appMetadata, layoutSet);
        var defaultDataElement = instance.Data.FirstOrDefault(d => d.DataType == defaultDataType?.Id);
        Debug.Assert(defaultDataElement is not null, "Default data element not found");

        return LayoutEvaluator.RunLayoutValidationsForRequired(evaluationState, defaultDataElement.Id);
    }

    internal static IEnumerable<DataElement> GetReferencedDataTypes(
        ApplicationMetadata appMetadata,
        LayoutSet? layoutSet,
        LayoutModel layouts,
        Instance instance,
        string taskId
    )
    {
        DataType? defaultDataType = DefaultDataType(taskId, appMetadata, layoutSet);
        if (defaultDataType is null)
        {
            return [];
        }

        var referencedDataTypes = layouts.HasExternalModelReferences();
        if (referencedDataTypes)
        {
            // Only request data for the defaultDataType
            return instance.Data.Where(d => d.DataType == defaultDataType.Id);
        }

        var allDataTypesWithLogic = appMetadata.DataTypes.Where(dt => dt.AppLogic?.ClassRef is not null).ToList();
        return instance.Data.Where(d => allDataTypesWithLogic.Any(dt => dt.Id == d.DataType)).ToList();
    }
}

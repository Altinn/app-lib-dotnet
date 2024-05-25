using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

/// <summary>
///     Run validations that require multiple data elements to be loaded for validation to be successful.
/// </summary>
public interface IMultipleFormDataValidator
{
    /// <summary>
    ///     The data type this validator is for.
    ///     To validate all types with form data, just use a "*" as value
    /// </summary>
    public string TaskId { get; }

    /// <summary>
    ///     What data elements this validator should run for
    /// </summary>
    public Task<IEnumerable<DataElement>> GetRequiredDataElementsForValidation(Instance instance, string taskId);

    /// <summary>
    ///     Used for partial validation to ensure that the validator only runs when relevant fields have changed.
    /// </summary>
    /// <param name="instance">The instance to validate</param>
    /// <param name="taskId">The current taskId </param>
    /// <param name="dataElement">The data element to validate</param>
    /// <param name="current">The current state of the form data in the currently updated data element</param>
    /// <param name="previous">The previous state of the form data in the currently updated data element</param>
    bool HasRelevantChanges(Instance instance, string taskId, DataElement dataElement, object current, object previous);

    /// <summary>
    ///     Returns the group id of the validator. This is used to run partial validations on the backend.
    ///     The default is based on the FullName and TaskId fields, and should not need customization
    /// </summary>
    public string ValidationSource => $"{GetType().FullName}-{TaskId}";

    /// <summary>
    ///     The actual validation function
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="taskId">The current task id to run validations for</param>
    /// <param name="data"></param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    /// <returns>List of validation issues</returns>
    Task<List<ValidationIssue>> ValidateFormData(
        Instance instance,
        string taskId,
        List<KeyValuePair<DataElement, object>> data,
        string? language
    );
}

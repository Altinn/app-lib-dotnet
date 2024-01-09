using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation;

/// <summary>
/// Core interface for validation of instances. Only a single implementation of this interface should exist in the app.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates the instance with all data elements on the current task and ensures that the instance is read for process next.
    /// </summary>
    /// <remarks>
    /// This method executes validations in the following interfaces
    /// * <see cref="ITaskValidator"/> for the current task
    /// * <see cref="IDataElementValidator"/> for all data elements on the current task
    /// * <see cref="IFormDataValidator"/> for all data elements with app logic on the current task
    /// </remarks>
    /// <param name="instance">The instance to validate</param>
    /// <param name="taskId">instance.Process?.CurrentTask?.ElementId</param>
    /// <returns>List of validation issues for this data element</returns>
    Task<List<ValidationIssue>> ValidateInstanceAtTask(Instance instance, string taskId);

    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// This method executes validations in the following interfaces
    /// * <see cref="IDataElementValidator"/> for all data elements on the current task
    /// * <see cref="IFormDataValidator"/> for all data elements with app logic on the current task
    ///
    /// This method does not run task validations
    /// </remarks>
    /// <param name="instance">The instance to validate</param>
    /// <param name="dataElement">The data element to run validations for</param>
    /// <param name="dataType">The data type (from applicationmetadata) that the element is an instance of</param>
    /// <returns>List of validation issues for this data element</returns>
    Task<List<ValidationIssue>> ValidateDataElement(Instance instance, DataElement dataElement, DataType dataType);

    /// <summary>
    /// Validates a single data element. Used by frontend to continuously validate form data as it changes.
    /// </summary>
    /// <remarks>
    /// This method executes validations for <see cref="IFormDataValidator"/>
    /// </remarks>
    /// <param name="instance">The instance to validate</param>
    /// <param name="dataElement">The data element to run validations for</param>
    /// <param name="dataType">The type of the data element</param>
    /// <param name="data">The data deserialized to the strongly typed object that represents the form data</param>
    /// <param name="changedFields">List of json paths for the fields that have changed (used for incremental validation)</param>
    /// <param name="ignoredValidators">List validators that should not be run (for incremental validation). Typically known validators that frontend knows how to replicate</param>
    Task<Dictionary<string, List<ValidationIssue>>> ValidateFormData(Instance instance, DataElement dataElement, DataType dataType, object data, List<string>? changedFields = null, List<string>? ignoredValidators = null);
}
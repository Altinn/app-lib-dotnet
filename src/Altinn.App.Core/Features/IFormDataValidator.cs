using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features;

/// <summary>
/// Interface for handling validation of form data. 
/// (i.e. dataElements with AppLogic defined 
/// </summary>
public interface IFormDataValidator
{
    /// <summary>
    /// The data type this validator is for. Typically either hard coded by implementation or
    /// or set by constructor using a <see cref="ServiceKeyAttribute" /> and a keyed service.
    ///
    /// To validate all types with form data, just use a "*" as value
    /// </summary>
    string DataType { get; }

    /// <summary>
    /// Used for partial validation to ensure that the validator only runs when relevant fields have changed.
    /// </summary>
    /// <param name="changedFields">List of the json path to all changed fields for incremental validation</param>
    bool ShouldRun(List<string> changedFields);

    /// <summary>
    /// Returns the group id of the validator. This is used to run partial validations on the backend.
    /// </summary>
    /// <remarks>
    /// The default implementation should work for most cases.
    /// </remarks>
    public string ValidationSource => $"{this.GetType().FullName}-{DataType}";

    /// <summary>
    /// The actual validation function
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="dataElement"></param>
    /// <param name="data"></param>
    /// <returns>List of validation issues</returns>
    Task<List<ValidationIssue>> ValidateFormData(Instance instance, DataElement dataElement, object data);
}
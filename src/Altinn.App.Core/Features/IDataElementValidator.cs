using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation;

/// <summary>
/// Validator for data elements.
/// See <see cref="IFormDataValidator"/> for an alternative validator for data elements with app logic.
/// and that support incremental validation on save.
/// For validating the content of files, see <see cref="IFileAnalyzer"/> and <see cref="IFileValidator"/>
/// </summary>
public interface IDataElementValidator
{
    /// <summary>
    /// The data type that this validator should run for. This is the id of the data type from applicationmetadata.json
    /// </summary>
    /// <remarks>
    /// Used by default in <see cref="CanValidateDataType"/>. Overrides might ignore this.
    /// </remarks>
    string DataType { get; }

    /// <summary>
    /// Override this method to customize what data elements this validator should run for.
    /// </summary>
    bool CanValidateDataType(DataType dataType)
    {
        return DataType == dataType.Id;
    }

    /// <summary>
    /// Run validations for a data element. This is supposed to run quickly
    /// </summary>
    /// <param name="instance">The instance to validate</param>
    /// <param name="dataElement"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    public Task<List<ValidationIssue>> ValidateDataElement(Instance instance, DataElement dataElement, DataType dataType);
}
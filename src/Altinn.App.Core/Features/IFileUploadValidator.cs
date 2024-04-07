using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

/// <summary>
/// Interface for handling validation of files when uploaded.
/// A validation failiure will cause the upload to be rejected.
/// </summary>
public interface IFileUploadValidator
{
    /// <summary>
    /// The data type that this validator should run for. This is the id of the data type from applicationmetadata.json
    /// The string "*" is a special value that means that the validator should run for all data types that don't have app-logic.
    /// </summary>
    string DataType { get; }

    /// <summary>
    /// Returns the group id of the validator.
    /// The default is based on the FullName and DataType fields, and should not need customization
    /// </summary>
    string ValidationSource => $"{this.GetType().FullName}-{DataType}";

    /// <summary>
    /// Validating a an uploaded file to possibly prevent the upload.
    /// The file is considered valid if the list of issues has no entries where <see cref="ValidationIssue.Severity"/> is Error.
    /// </summary>
    Task<List<ValidationIssue>> Validate(Instance instance, DataType dataType, byte[] fileContent, string? filename, string? mimeType, string? language);
}

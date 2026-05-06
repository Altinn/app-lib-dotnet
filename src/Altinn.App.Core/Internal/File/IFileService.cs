using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.File;

/// <summary>
/// File service
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Rund file analysis and validation
    /// </summary>
    /// <param name="dataTypeFromMetadata">Data type found in application metadata</param>
    /// <param name="bytes">Byte data</param>
    /// <param name="fileName">File name</param>
    /// <returns></returns>
    Task<List<ValidationIssueWithSource>?> RunFileAnalysisAndValidation(
        DataType dataTypeFromMetadata,
        byte[] bytes,
        string? fileName
    );
}

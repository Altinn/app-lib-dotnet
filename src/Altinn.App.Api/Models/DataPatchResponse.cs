#nullable disable
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Internal.Patch;
using Altinn.App.Core.Models.Validation;

namespace Altinn.App.Api.Models;

/// <summary>
/// Represents the response from a data patch operation on the <see cref="DataController"/>.
/// </summary>
public class DataPatchResponse
{
    /// <summary>
    /// The validation issues that were found during the patch operation.
    /// </summary>
    public required Dictionary<string, List<ValidationIssue>> ValidationIssues { get; init; }

    /// <summary>
    /// The current data model after the patch operation.
    /// </summary>
    public required object NewDataModel { get; init; }
    
    /// <summary>
    /// Implicitly converts a <see cref="DataPatchResult"/> to a <see cref="DataPatchResponse"/>.
    /// </summary>
    /// <returns></returns>
    public static implicit operator DataPatchResponse(DataPatchResult result)
    {
        return new DataPatchResponse
        {
            ValidationIssues = result.ValidationIssues,
            NewDataModel = result.NewDataModel
        };
    }
    
    /// <summary>
    /// Implicitly converts a <see cref="DataPatchResponse"/> to a <see cref="DataPatchResult"/>.
    /// </summary>
    /// <returns></returns>
    public static implicit operator DataPatchResult(DataPatchResponse response)
    {
        return new DataPatchResult
        {
            ValidationIssues = response.ValidationIssues,
            NewDataModel = response.NewDataModel
        };
    }
}
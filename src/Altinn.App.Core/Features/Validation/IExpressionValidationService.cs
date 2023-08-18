using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Validation
{
    /// <summary>
    /// Interface for running all file validators registered on a data type.
    /// </summary>
    public interface IExpressionValidationService
    {
        /// <summary>
        /// Validates the file based on the file analysis results.
        /// </summary>
        Task<(bool Success, List<ValidationIssue> Errors)> Validate(string dataType);
    }
}

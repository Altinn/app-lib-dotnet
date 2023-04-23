using Altinn.App.Core.Features.FileAnalysis;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Core.Features.Validation
{
    /// <summary>
    /// Interface for handling validation of files added to an instance.
    /// </summary>
    public interface IFileValidation
    {
        /// <summary>
        /// Validates
        /// </summary>
        Task<(bool Success, ActionResult Errors)> Validate(DataType dataType, FileAnalysisResult fileAnalysisResult);
    }
}

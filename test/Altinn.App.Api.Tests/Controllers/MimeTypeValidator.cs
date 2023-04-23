using Altinn.App.Core.Features.FileAnalysis;
using Altinn.App.Core.Features.Validation;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Api.Tests.Controllers
{
    public class MimeTypeValidator : IFileValidator
    {
        public string Id { get; private set; } = "mimeTypeValidator";

        public async Task<(bool Success, List<ValidationIssue> Errors)> Validate(DataType dataType, List<FileAnalysisResult> fileAnalysisResults)
        {
            // TODO: Get texts from somewhere

            Dictionary<string, Dictionary<string, string>> serviceText = new Dictionary<string, Dictionary<string, string>>();

            List<ValidationIssue> errors = new();

            var fileMimeTypeResult = fileAnalysisResults.FirstOrDefault(x => x.MimeType !=  null);

            // Verify that file mime type is an allowed content-type
            if (!dataType.AllowedContentTypes.Contains(fileMimeTypeResult.MimeType, StringComparer.InvariantCultureIgnoreCase) && !dataType.AllowedContentTypes.Contains("application/octet-stream"))
            {
                ValidationIssue error = new ValidationIssue
                {
                    Code = ValidationIssueCodes.DataElementCodes.ContentTypeNotAllowed,
                    //InstanceId = instance.Id,
                    Severity = ValidationIssueSeverity.Error,
                    //DataElementId = element.Id,
                    Description = AppTextHelper.GetAppText(ValidationIssueCodes.DataElementCodes.ContentTypeNotAllowed, serviceText, null, "nb")
                };

                errors.Add(error);

                //errorResponse = new BadRequestObjectResult($"{errorBaseMessage} Invalid content type: {fileAnalysisResults.MimeType}. Please try another file. Permitted content types include: {string.Join(", ", dataType.AllowedContentTypes)}");
                return (false, errors);
            }

            return (true, errors);
        }
    }
}

namespace Altinn.App.Core.Features.Validation.Default;

using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.App.Core.Features.FileAnalysis;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

/// <summary>
/// Implementation of <see cref="IFileUploadValidator"/> that uses the legacy
/// <see cref="IFileValidator"/> and <see cref="IFileAnalyser"/>.
/// </summary>
public class LegacyFileAnalyzerValidator : IFileUploadValidator
{
    private readonly IEnumerable<IFileValidator> _fileValidators;
    private readonly IEnumerable<IFileAnalyser> _fileAnalysers;

    /// <summary>
    /// Constructor for depency injection.
    /// </summary>
    public LegacyFileAnalyzerValidator(
        IEnumerable<IFileValidator> fileValidators,
        IEnumerable<IFileAnalyser> fileAnalysers
    )
    {
        _fileValidators = fileValidators;
        _fileAnalysers = fileAnalysers;
    }

    /// <inheritdoc/>
    public string DataType => "*";

    /// <inheritdoc/>
    public async Task<List<ValidationIssue>> Validate(
        Instance instance,
        DataType dataType,
        byte[] fileContent,
        string? filename,
        string? mimeType,
        string? language
    )
    {
        var fileAnalysisResults = new List<FileAnalysisResult>();
        if (dataType.EnabledFileAnalysers?.Count > 0)
        {
            // Run file analysis
            foreach (var analyserName in dataType.EnabledFileAnalysers)
            {
                var analyser =
                    _fileAnalysers.FirstOrDefault(a => a.Id == analyserName)
                    ?? throw new InvalidOperationException("Could not find file analyzer with ID: " + analyserName);
                using var stream = new MemoryStream(fileContent);
                var result = await analyser.Analyse(stream, filename);
                result.AnalyserId = analyser.Id;
                result.Filename = filename;
                fileAnalysisResults.Add(result);
            }
        }

        if (dataType.EnabledFileValidators?.Count > 0)
        {
            // Run file validation
            var validationIssues = new List<ValidationIssue>();
            foreach (var validatorName in dataType.EnabledFileValidators)
            {
                var validator = _fileValidators.First(v => v.Id == validatorName);
                var (success, issues) = await validator.Validate(dataType, fileAnalysisResults);
                if (!success)
                {
                    validationIssues.AddRange(issues);
                }
            }

            return validationIssues;
        }
        return new List<ValidationIssue>();
    }
}

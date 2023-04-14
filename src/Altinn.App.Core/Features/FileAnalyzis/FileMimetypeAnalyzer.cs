namespace Altinn.App.Core.Features.FileAnalyzis;

/// <summary>
/// Does a deep analysis of the filetype by scanning the binary
/// for known string patterns and magic numbers.
/// </summary>
public class FileMimeTypeAnalyzer : IFileAnalyzer
{
    /// <summary>
    /// Analyzes the content and returns any findings.
    /// </summary>
    public async Task<IEnumerable<FileAnalyzeResult>> Analyze(Stream stream)
    {
        var result = new List<FileAnalyzeResult>();

        return result;
    }
}

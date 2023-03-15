namespace Altinn.App.Core.Features.FileAnalyzis;

/// <summary>
/// Does a deep analysis of the filetype by scanning the binary
/// for known string patterns and magic numbers.
/// </summary>
public class FileMimeTypeAnalyzer : IFileAnalyzer
{
    /// <summary>
    /// Analyzes the content and returns any findings as name/value pairs.
    /// </summary>
    public IDictionary<string, string> Analyze(StreamContent streamContent)
    {
        Dictionary<string, string> metadata = new Dictionary<string, string>();

        return metadata;
    }
}

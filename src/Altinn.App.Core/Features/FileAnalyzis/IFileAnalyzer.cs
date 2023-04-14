namespace Altinn.App.Core.Features.FileAnalyzis
{
    /// <summary>
    /// Interface for doing extended binary file analyzing.
    /// </summary>
    public interface IFileAnalyzer
    {
        /// <summary>
        /// Analyses a stream with the intent to extract metadata.
        /// </summary>
        /// <param name="stream">The stream to analyze. One stream = one file.</param>
        public Task<IEnumerable<FileAnalyzeResult>> Analyze(Stream stream);
    }
}

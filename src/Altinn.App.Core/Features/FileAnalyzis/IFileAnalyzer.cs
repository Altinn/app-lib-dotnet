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
        /// <param name="streamContent">The stream content to analyze</param>
        /// <returns>A <see cref="IDictionary{TKey, TValue}" /> of key/value pairs containing the metadata extracted from the stream during the analysis.</returns>
        public IDictionary<string, string> Analyze(StreamContent streamContent);
    }
}

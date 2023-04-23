namespace Altinn.App.Core.Features.FileAnalysis
{
    /// <summary>
    /// Interface for doing extended binary file analysing.
    /// </summary>
    public interface IFileAnalyser
    {
        /// <summary>
        /// Analyses a stream with the intent to extract metadata.
        /// </summary>
        /// <param name="stream">The stream to analyse. One stream = one file.</param>
        /// <param name="filename">Filename. Optional parameter if the implementation needs the name of the file, relative or absolute path.</param>
        public Task<IEnumerable<FileAnalysisResult>> Analyse(Stream stream, string? filename = null);
    }
}

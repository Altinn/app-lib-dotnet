using Altinn.App.Core.Features.FileAnalysis;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.FileAnalyzis
{
    /// <summary>
    /// Analyses a file using the registred analysers on the <see cref="DataType"/>
    /// </summary>
    public class FileAnalyserService : IFileAnalyserService
    {
        private readonly IFileAnalyserFactory _fileAnalyserFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileAnalyserService"/> class.
        /// </summary>
        public FileAnalyserService(IFileAnalyserFactory fileAnalyserFactory)
        {
            _fileAnalyserFactory = fileAnalyserFactory;
        }

        /// <summary>
        /// Runs the specified file analysers against the stream provided.
        /// </summary>
        public async Task<IEnumerable<FileAnalysisResult>> Analyse(DataType dataType, Stream fileStream, string? filename)
        {
            List<IFileAnalyser> fileAnalysers = _fileAnalyserFactory.GetFileAnalysers(dataType.EnabledFileAnalysers).ToList();

            List<FileAnalysisResult> fileAnalysisResults = new();
            foreach (var analyser in fileAnalysers)
            {
                fileAnalysisResults.Add(await analyser.Analyse(fileStream, filename));
            }

            return fileAnalysisResults;
        }
    }
}

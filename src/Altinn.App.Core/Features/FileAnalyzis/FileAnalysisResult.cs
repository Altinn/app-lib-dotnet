namespace Altinn.App.Core.Features.FileAnalysis
{
    /// <summary>
    /// Results from a file analysis done based the content of the file, ie. the binary data.
    /// </summary>
    public class FileAnalysisResult
    {
        /// <summary>
        /// The name of the analysed file either filename or some other identifier.
        /// </summary>
        public string? Filename { get; set; }

        /// <summary>
        /// The file extension(s) without the . i.e. pdf | png | docx
        /// Some files might have multiple extensions.
        /// </summary>
        public List<string> Extensions { get; set; } = new List<string>();

        /// <summary>
        /// The mime type
        /// </summary>
        public string? MimeType { get; set; }
        
        /// <summary>
        /// Key/Value pairs containg analyse findings. eg. mimetype | application/pdf
        /// depending on the file analysed.
        /// </summary>
        public IDictionary<string, string> Metadata { get; private set; } = new Dictionary<string, string>();
    }
}

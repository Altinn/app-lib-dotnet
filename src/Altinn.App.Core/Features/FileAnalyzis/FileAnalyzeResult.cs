namespace Altinn.App.Core.Features.FileAnalyzis
{
    /// <summary>
    /// Results from a file analyzis done based the content of the file, ie. the binary data.
    /// </summary>
    public class FileAnalyzeResult
    {
        /// <summary>
        /// The name of the analyzed file either filename or some other identifier.
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
        /// Key/Value pairs containg analyzis findings. eg. mimetype | application/pdf
        /// depending on the file analyzed.
        /// </summary>
        public IDictionary<string, string> Metadata { get; private set; } = new Dictionary<string, string>();
    }
}

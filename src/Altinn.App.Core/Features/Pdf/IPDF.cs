using Altinn.App.Services.Models;

namespace Altinn.App.Core.Features.Pdf
{
    /// <summary>
    /// The pdf service
    /// </summary>
    public interface IPDF
    {
        /// <summary>
        /// Generates a pdf receipt for a given dataElement
        /// </summary>
        Task<Stream> GeneratePDF(PDFContext pdfContext);
    }
}

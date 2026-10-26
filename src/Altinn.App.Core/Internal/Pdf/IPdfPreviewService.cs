using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Pdf;

/// <summary>
/// Renders a PDF service task without storing a PDF or advancing the process.
/// Kept separate from the public v8 PDF service contract.
/// </summary>
internal interface IPdfPreviewService
{
    Task<Stream> GeneratePreview(
        Instance instance,
        string taskId,
        string? language,
        SubformPdfContext? subformPdfContext,
        List<string>? autoGeneratePdfForTaskIds,
        CancellationToken cancellationToken
    );
}

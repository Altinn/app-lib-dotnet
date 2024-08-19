using Altinn.App.Core.Internal.Pdf;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process.ServiceTasks;

/// <summary>
/// Service task that generates PDFs for tasks specified in the process configuration.
/// </summary>
public class PdfServiceTaskV2 : ServiceTaskBase
{
    private readonly IPdfService _pdfService;
    private readonly ILogger<PdfServiceTaskV2> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfServiceTask"/> class.
    /// </summary>
    public PdfServiceTaskV2(ILogger<PdfServiceTaskV2> logger, IPdfService pdfService)
    {
        _logger = logger;
        _pdfService = pdfService;
    }

    /// <inheritdoc />
    public override string Type => "pdf";

    /// <inheritdoc/>
    protected override async Task Execute(string taskId, Instance instance)
    {
        _logger.LogDebug("Calling PdfService for PDF Service Task {TaskId}.", taskId);
        await _pdfService.GenerateAndStorePdf(instance, taskId, CancellationToken.None);
        _logger.LogDebug("Successfully called PdfService for PDF Service Task {TaskId}.", taskId);
    }
}

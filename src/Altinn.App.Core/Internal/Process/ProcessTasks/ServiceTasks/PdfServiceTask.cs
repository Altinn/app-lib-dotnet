using Altinn.App.Core.Internal.Pdf;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

internal interface IPdfServiceTask : IServiceTask { }

/// <summary>
/// Service task that generates PDFs for tasks specified in the process configuration.
/// </summary>
public class PdfServiceTask : IPdfServiceTask
{
    private readonly IPdfService _pdfService;
    private readonly ILogger<PdfServiceTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfServiceTask"/> class.
    /// </summary>
    public PdfServiceTask(ILogger<PdfServiceTask> logger, IPdfService pdfService)
    {
        _logger = logger;
        _pdfService = pdfService;
    }

    /// <inheritdoc />
    public string Type => "pdf";

    /// <inheritdoc/>
    public async Task Execute(string taskId, Instance instance)
    {
        _logger.LogDebug("Calling PdfService for PDF Service Task {TaskId}.", taskId);
        await _pdfService.GenerateAndStorePdf(instance, taskId, CancellationToken.None);
        _logger.LogDebug("Successfully called PdfService for PDF Service Task {TaskId}.", taskId);
    }

    /// <inheritdoc />
    public Task Start(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task End(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Abandon(string taskId, Instance instance)
    {
        return Task.CompletedTask;
    }
}

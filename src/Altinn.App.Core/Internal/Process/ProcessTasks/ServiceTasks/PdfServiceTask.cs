using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
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
    private readonly IProcessReader _processReader;
    private readonly ILogger<PdfServiceTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfServiceTask"/> class.
    /// </summary>
    public PdfServiceTask(IPdfService pdfService, IProcessReader processReader, ILogger<PdfServiceTask> logger)
    {
        _logger = logger;
        _pdfService = pdfService;
        _processReader = processReader;
    }

    /// <inheritdoc />
    public string Type => "pdf";

    /// <inheritdoc/>
    public async Task Execute(string taskId, Instance instance, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calling PdfService for PDF Service Task {TaskId}.", taskId);

        ValidAltinnPdfConfiguration config = GetValidAltinnPdfConfiguration(taskId);
        await _pdfService.GenerateAndStorePdf(instance, taskId, config.Filename, cancellationToken);

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

    private ValidAltinnPdfConfiguration GetValidAltinnPdfConfiguration(string taskId)
    {
        AltinnTaskExtension? altinnTaskExtension = _processReader.GetAltinnTaskExtension(taskId);
        AltinnPdfConfiguration? pdfConfiguration = altinnTaskExtension?.PdfConfiguration;

        if (pdfConfiguration == null)
        {
            // If no PDF configuration is specified, return a default valid configuration. No required config as of now.
            return new ValidAltinnPdfConfiguration();
        }

        return pdfConfiguration.Validate();
    }
}

using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements;
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
    public PdfServiceTask(ILogger<PdfServiceTask> logger, IPdfService pdfService, IProcessReader processReader)
    {
        _logger = logger;
        _pdfService = pdfService;
        _processReader = processReader;
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

    private ValidAltinnPdfConfiguration GetValidAltinnPdfConfiguration(string taskId)
    {
        AltinnTaskExtension? altinnTaskExtension = _processReader.GetAltinnTaskExtension(taskId);
        AltinnPdfConfiguration? pdfConfiguration = altinnTaskExtension?.PdfConfiguration;

        if (pdfConfiguration == null)
        {
            throw new ApplicationConfigException("PdfConfig is missing in the PDF service task configuration.");
        }

        return pdfConfiguration.Validate();
    }

    /// <inheritdoc />
    public Task Start(string taskId, Instance instance)
    {
        ValidAltinnPdfConfiguration config = GetValidAltinnPdfConfiguration(taskId);

        foreach (string pdfTaskId in config.TaskIds)
        {
            ProcessTask? processElement = _processReader.GetProcessTasks().FirstOrDefault(x => x.Id == pdfTaskId);
            if (processElement == null)
            {
                throw new ProcessException(
                    $"The task ID {pdfTaskId} specified in the PDF configuration does not exist in the process BPMN."
                );
            }
        }

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

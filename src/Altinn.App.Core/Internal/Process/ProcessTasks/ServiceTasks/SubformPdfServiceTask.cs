using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

internal class SubformPdfServiceTask(
    IProcessReader processReader,
    IPdfService pdfService,
    IDataClient dataClient,
    IProcessTaskCleaner processTaskCleaner,
    ILogger<SubformPdfServiceTask> logger
) : IServiceTask
{
    public string Type => "subform-pdf";

    public async Task<ServiceTaskResult> Execute(ServiceTaskContext context)
    {
        string taskId = context.InstanceDataMutator.Instance.Process.CurrentTask.ElementId;
        Instance instance = context.InstanceDataMutator.Instance;

        logger.LogDebug("Calling PdfService for PDF Service Task {TaskId}.", taskId);

        ValidAltinnSubformPdfConfiguration config = GetValidAltinnSubformPdfConfiguration(taskId);

        string? filenameTextResourceKey = config.FilenameTextResourceKey;
        string subformComponentId = config.SubformComponentId;
        string subformDataTypeId = config.SubformDataTypeId;
        bool parallelExecution = config.ParallelExecution;

        // Clean up any existing PDFs from previous failed attempts
        await processTaskCleaner.RemoveAllDataElementsGeneratedFromTask(instance, taskId);

        List<DataElement> subformDataElements = instance.Data.Where(x => x.DataType == subformDataTypeId).ToList();

        if (parallelExecution)
        {
            // Generate PDFs in parallel for better performance when PDF microservice can handle concurrent requests
            var pdfTasks = subformDataElements.Select(async dataElement =>
            {
                DataElement pdfDataElement = await pdfService.GenerateAndStoreSubformPdf(
                    instance,
                    taskId,
                    filenameTextResourceKey,
                    subformComponentId,
                    dataElement.Id,
                    context.CancellationToken
                );

                await AddSubformPdfMetadata(
                    instance,
                    pdfDataElement,
                    subformComponentId,
                    dataElement.Id,
                    context.CancellationToken
                );
            });

            await Task.WhenAll(pdfTasks);
        }
        else
        {
            // Generate PDFs sequentially to avoid overwhelming the PDF microservice
            foreach (DataElement dataElement in subformDataElements)
            {
                DataElement pdfDataElement = await pdfService.GenerateAndStoreSubformPdf(
                    instance,
                    taskId,
                    filenameTextResourceKey,
                    subformComponentId,
                    dataElement.Id,
                    context.CancellationToken
                );

                await AddSubformPdfMetadata(
                    instance,
                    pdfDataElement,
                    subformComponentId,
                    dataElement.Id,
                    context.CancellationToken
                );
            }
        }

        logger.LogDebug("Successfully called PdfService for PDF Service Task {TaskId}.", taskId);

        return new ServiceTaskSuccessResult();
    }

    private ValidAltinnSubformPdfConfiguration GetValidAltinnSubformPdfConfiguration(string taskId)
    {
        AltinnTaskExtension? altinnTaskExtension = processReader.GetAltinnTaskExtension(taskId);
        AltinnSubformPdfConfiguration? subformPdfConfiguration = altinnTaskExtension?.SubformPdfConfiguration;

        if (subformPdfConfiguration == null)
        {
            // If no PDF configuration is specified, return a default valid configuration. No required config as of now.
            throw new ApplicationConfigException(
                "The subformPdfConfig node is missing in the subform pdf process task configuration."
            );
        }

        return subformPdfConfiguration.Validate();
    }

    private async Task AddSubformPdfMetadata(
        Instance instance,
        DataElement pdfDataElement,
        string subformComponentId,
        string subformDataElementId,
        CancellationToken ct
    )
    {
        pdfDataElement.Metadata = new List<KeyValueEntry>
        {
            new() { Key = "subformComponentId", Value = subformComponentId },
            new() { Key = "subformDataElementId", Value = subformDataElementId },
        };

        await dataClient.Update(instance, pdfDataElement, cancellationToken: ct);
    }
}

using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

internal class SubformPdfServiceTask(
    IProcessReader processReader,
    IPdfService pdfService,
    IDataClient dataClient,
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

        List<DataElement> subformDataElements = instance.Data.Where(x => x.DataType == subformDataTypeId).ToList();

        // Clean up any existing subform PDFs from previous failed attempts
        await CleanupExistingSubformPdfs(context, subformComponentId, subformDataElements);

        if (parallelExecution)
        {
            // Generate PDFs in parallel for better performance when PDF microservice can handle concurrent requests
            var pdfTasks = subformDataElements.Select(dataElement =>
                pdfService.GenerateAndStoreSubformPdfs(
                    instance,
                    taskId,
                    filenameTextResourceKey,
                    subformComponentId,
                    dataElement.Id,
                    context.CancellationToken
                )
            );

            await Task.WhenAll(pdfTasks);
        }
        else
        {
            // Generate PDFs sequentially to avoid overwhelming the PDF microservice
            foreach (DataElement dataElement in subformDataElements)
            {
                await pdfService.GenerateAndStoreSubformPdfs(
                    instance,
                    taskId,
                    filenameTextResourceKey,
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
        AltinnSubformPdfConfiguration? pdfConfiguration = altinnTaskExtension?.SubformPdfConfiguration;

        if (pdfConfiguration == null)
        {
            // If no PDF configuration is specified, return a default valid configuration. No required config as of now.
            return new ValidAltinnSubformPdfConfiguration();
        }

        return pdfConfiguration.Validate();
    }

    private async Task CleanupExistingSubformPdfs(
        ServiceTaskContext context,
        string subformComponentId,
        List<DataElement> subformDataElements
    )
    {
        try
        {
            Instance instance = context.InstanceDataMutator.Instance;

            // Find existing PDF data elements that might be from previous failed attempts
            List<DataElement> existingPdfs = instance
                .Data.Where(d => d.DataType == "ref-data-as-pdf")
                .Where(d => HasSubformMetadata(d, subformComponentId, subformDataElements))
                .ToList();

            if (existingPdfs.Count > 0)
            {
                logger.LogInformation(
                    "Found {Count} existing subform PDFs to clean up for component {ComponentId} in instance {InstanceId}",
                    existingPdfs.Count,
                    subformComponentId,
                    instance.Id
                );

                var instanceIdentifier = new InstanceIdentifier(instance);
                var appIdentifier = new AppIdentifier(instance);

                foreach (DataElement? pdf in existingPdfs)
                {
                    try
                    {
                        await dataClient.DeleteData(
                            instanceIdentifier.InstanceOwnerPartyId,
                            instanceIdentifier.InstanceGuid,
                            Guid.Parse(pdf.Id),
                            delay: false, // Delete immediately
                            cancellationToken: context.CancellationToken
                        );

                        logger.LogDebug(
                            "Deleted existing subform PDF {DataElementId} from instance {InstanceId}",
                            pdf.Id,
                            instance.Id
                        );
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Failed to delete existing subform PDF {DataElementId} from instance {InstanceId}",
                            pdf.Id,
                            instance.Id
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error during subform PDF cleanup in instance {InstanceId}",
                context.InstanceDataMutator.Instance.Id
            );
        }
    }

    private async Task AddSubformPdfMetadata(
        Instance instance,
        string dataElementId,
        string subformComponentId,
        string subformDataElementId,
        CancellationToken ct
    )
    {
        DataElement? dataElement = instance.Data.FirstOrDefault(d => d.Id == dataElementId);

        if (dataElement == null)
        {
            throw new InvalidOperationException($"DataElement {dataElementId} not found in instance {instance.Id}");
        }

        dataElement.Metadata = new List<KeyValueEntry>
        {
            new() { Key = "subformComponentId", Value = subformComponentId },
            new() { Key = "subformDataElementId", Value = subformDataElementId },
        };

        await dataClient.Update(instance, dataElement, cancellationToken: ct);
    }

    private static bool HasSubformMetadata(
        DataElement dataElement,
        string subformComponentId,
        List<DataElement> subformDataElements
    )
    {
        // Check if this PDF has metadata indicating it's from our subform component
        if (dataElement.Metadata?.Any(m => m.Key == "subformComponentId" && m.Value == subformComponentId) == true)
        {
            return true;
        }

        // Fallback: Check if this PDF has metadata indicating it's from any of our subform data elements
        List<string> subformDataElementIds = subformDataElements.Select(d => d.Id).ToList();
        return dataElement.Metadata?.Any(m =>
                m.Key == "subformDataElementId" && subformDataElementIds.Contains(m.Value)
            ) == true;
    }
}

using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.FeatureManagement;

namespace Altinn.App.Core.Internal.Process.ServiceTasks;

/// <summary>
/// Service task that generates PDFs for all connected datatypes that have the EnablePdfCreation flag set to true.
/// </summary>
/// <param name="appMetadata"></param>
/// <param name="featureManager"></param>
/// <param name="pdfService"></param>
/// <param name="appModel"></param>
public class PdfServiceTask(IAppMetadata appMetadata, IFeatureManager featureManager, IPdfService pdfService, IAppModel appModel) : IServiceTask
{
    private readonly IAppMetadata _appMetadata = appMetadata;

    /// <summary>
    /// Executes the service task.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public async Task Execute(string taskId, Instance instance)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        List<DataType> connectedDataTypes = appMetadata.DataTypes.FindAll(dt => dt.TaskId == taskId);
        foreach (DataType dataType in connectedDataTypes)
        {
            bool generatePdf = dataType.AppLogic?.ClassRef != null && dataType.EnablePdfCreation;

            foreach (DataElement dataElement in instance.Data.FindAll(de => de.DataType == dataType.Id))
            {
                if (generatePdf)
                {
                    if (await featureManager.IsEnabledAsync(FeatureFlags.NewPdfGeneration))
                    {
                        await pdfService.GenerateAndStorePdf(instance, taskId, CancellationToken.None);
                    }
                    else
                    {
                        Type dataElementType = appModel.GetModelType(dataType.AppLogic.ClassRef);
                        await pdfService.GenerateAndStoreReceiptPDF(instance, taskId, dataElement, dataElementType);
                    }
                }
            }
        }
    }
}
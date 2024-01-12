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
public class PdfServiceTask : IServiceTask
{
    private readonly IAppMetadata _appMetadata;
    private readonly IFeatureManager _featureManager;
    private readonly IPdfService _pdfService;
    private readonly IAppModel _appModel;

    /// <summary>
    /// Service task that generates PDFs for all connected datatypes that have the EnablePdfCreation flag set to true.
    /// </summary>
    /// <param name="appMetadata"></param>
    /// <param name="featureManager"></param>
    /// <param name="pdfService"></param>
    /// <param name="appModel"></param>
    public PdfServiceTask(IAppMetadata appMetadata, IFeatureManager featureManager, IPdfService pdfService,
        IAppModel appModel)
    {
        _featureManager = featureManager;
        _pdfService = pdfService;
        _appModel = appModel;
        _appMetadata = appMetadata;
    }

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
        var newPdfServiceEnabled = _featureManager.IsEnabledAsync(FeatureFlags.NewPdfGeneration);
        foreach (DataType dataType in connectedDataTypes)
        {
            bool generatePdf = dataType.AppLogic?.ClassRef != null && dataType.EnablePdfCreation;
            if (generatePdf && await newPdfServiceEnabled && instance.Data.Exists(de => de.DataType == dataType.Id))
            {
                await _pdfService.GenerateAndStorePdf(instance, taskId, CancellationToken.None);
                return;
            }

            foreach (DataElement dataElement in instance.Data.FindAll(de => de.DataType == dataType.Id))
            {
                if (generatePdf)
                {
                    Type dataElementType = _appModel.GetModelType(dataType.AppLogic.ClassRef);
                    await _pdfService.GenerateAndStoreReceiptPDF(instance, taskId, dataElement, dataElementType);
                }
            }
        }
    }
}
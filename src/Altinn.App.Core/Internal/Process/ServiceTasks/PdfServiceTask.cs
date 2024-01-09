using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.App.Core.Internal.Process.ServiceTasks;

public class PdfServiceTask: IServiceTask
{
    private readonly IAppMetadata _appMetadata;
    private readonly IFeatureManager _featureManager;
    private readonly IPdfService _pdfService;
    private readonly IAppModel _appModel;

    public PdfServiceTask(IAppMetadata appMetadata, IFeatureManager featureManager, IPdfService pdfService, IAppModel appModel)
    {
        _appMetadata = appMetadata;
        _featureManager = featureManager;
        _pdfService = pdfService;
        _appModel = appModel;
    }


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
                    if (await _featureManager.IsEnabledAsync(FeatureFlags.NewPdfGeneration))
                    {
                        await _pdfService.GenerateAndStorePdf(instance, taskId, CancellationToken.None);
                    }
                    else
                    {
                        Type dataElementType = _appModel.GetModelType(dataType.AppLogic.ClassRef);
                        await _pdfService.GenerateAndStoreReceiptPDF(instance, taskId, dataElement, dataElementType);
                    }
                }
            }
        }
    }
}
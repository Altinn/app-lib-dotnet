using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Implementation;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Prefill;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process;

public class ProcessTaskStartCommonLogic
{
    //Start
    private readonly ILogger<ProcessTaskStartCommonLogic> _logger;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly IPrefill _prefillService;
    private readonly IAppModel _appModel;
    private readonly IInstantiationProcessor _instantiationProcessor;
    private readonly IInstanceClient _instanceClient;

    public ProcessTaskStartCommonLogic(ILogger<ProcessTaskStartCommonLogic> logger,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IPrefill prefillService,
        IAppModel appModel,
        IInstantiationProcessor instantiationProcessor,
        IInstanceClient instanceClient
    )
    {
        _logger = logger;
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _prefillService = prefillService;
        _appModel = appModel;
        _instantiationProcessor = instantiationProcessor;
        _instanceClient = instanceClient;
    }


    public async Task Start(string taskId, Instance instance, Dictionary<string, string> prefill)
    {
        _logger.LogDebug("OnStartProcessTask for {InstanceId}", instance.Id);

        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();

        foreach (DataType dataType in appMetadata.DataTypes.Where(dt =>
                     dt.TaskId == taskId && dt.AppLogic?.AutoCreate == true))
        {
            _logger.LogDebug("Auto create data element: {DataTypeId}", dataType.Id);

            DataElement? dataElement = instance.Data.Find(d => d.DataType == dataType.Id);

            if (dataElement == null)
            {
                dynamic data = _appModel.Create(dataType.AppLogic.ClassRef);

                // runs prefill from repo configuration if config exists
                await _prefillService.PrefillDataModel(instance.InstanceOwner.PartyId, dataType.Id, data, prefill);
                await _instantiationProcessor.DataCreation(instance, data, prefill);

                Type type = _appModel.GetModelType(dataType.AppLogic.ClassRef);

                DataElement createdDataElement =
                    await _dataClient.InsertFormData(instance, dataType.Id, data, type);
                instance.Data.Add(createdDataElement);

                await UpdatePresentationTextsOnInstance(instance, dataType.Id, data);
                await UpdateDataValuesOnInstance(instance, dataType.Id, data);

                _logger.LogDebug("Created data element: {CreatedDataElementId}", createdDataElement.Id);
            }
        }
    }
    
    private async Task UpdatePresentationTextsOnInstance(Instance instance, string dataType, dynamic data)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        var updatedValues = DataHelper.GetUpdatedDataValues(
            appMetadata?.PresentationFields,
            instance.PresentationTexts,
            dataType,
            data);

        if (updatedValues.Count > 0)
        {
            var updatedInstance = await _instanceClient.UpdatePresentationTexts(
                int.Parse(instance.Id.Split("/")[0]),
                Guid.Parse(instance.Id.Split("/")[1]),
                new PresentationTexts { Texts = updatedValues });

            instance.PresentationTexts = updatedInstance.PresentationTexts;
        }
    }

    private async Task UpdateDataValuesOnInstance(Instance instance, string dataType, object data)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        var updatedValues = DataHelper.GetUpdatedDataValues(
            appMetadata?.DataFields,
            instance.DataValues,
            dataType,
            data);

        if (updatedValues.Count > 0)
        {
            var updatedInstance = await _instanceClient.UpdateDataValues(
                int.Parse(instance.Id.Split("/")[0]),
                Guid.Parse(instance.Id.Split("/")[1]),
                new DataValues { Values = updatedValues });

            instance.DataValues = updatedInstance.DataValues;
        }
    }
    
    
}
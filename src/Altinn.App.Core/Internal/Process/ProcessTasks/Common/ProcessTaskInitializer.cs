using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Prefill;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process.ProcessTasks;

/// <summary>
/// Contains common logic for starting a process task
/// </summary>
public class ProcessTaskInitializer : IProcessTaskInitializer
{
    private readonly ILogger<ProcessTaskInitializer> _logger;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly IPrefill _prefillService;
    private readonly IAppModel _appModel;
    private readonly IInstantiationProcessor _instantiationProcessor;
    private readonly IInstanceClient _instanceClient;

    /// <summary>
    /// Contains common logic for starting a process task
    /// </summary>
    public ProcessTaskInitializer(ILogger<ProcessTaskInitializer> logger,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IPrefill prefillService,
        IAppModel appModel,
        IInstantiationProcessor instantiationProcessor,
        IInstanceClient instanceClient)
    {
        _logger = logger;
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _prefillService = prefillService;
        _appModel = appModel;
        _instantiationProcessor = instantiationProcessor;
        _instanceClient = instanceClient;
    }

    /// <summary>
    /// Runs common "start" logic for process tasks for a given task ID and instance. This method initializes the data elements for the instance based on application metadata and prefill configurations. Also updates presentation texts and data values on the instance.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <param name="prefill"></param>
    public async Task Initialize(string taskId, Instance instance, Dictionary<string, string> prefill)
    {
        _logger.LogDebug("OnStartProcessTask for {InstanceId}", instance.Id);

        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

        foreach (DataType dataType in applicationMetadata.DataTypes.Where(dt =>
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

    public async Task UpdatePresentationTextsOnInstance(Instance instance, string dataType, dynamic data)
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        var updatedValues = DataHelper.GetUpdatedDataValues(
            applicationMetadata?.PresentationFields,
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

    public async Task UpdateDataValuesOnInstance(Instance instance, string dataType, object data)
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        var updatedValues = DataHelper.GetUpdatedDataValues(
            applicationMetadata?.DataFields,
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
using Altinn.App.Core.Configuration;
using Altinn.App.Core.EFormidling.Interface;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Altinn.App.Core.Implementation;

/// <summary>
/// Default handling of task process events.
/// </summary>
public class DefaultTaskEvents : ITaskEvents
{
    private readonly ILogger<DefaultTaskEvents> _logger;
    private readonly IAppResources _appResources;
    private readonly IAppMetadata _appMetadata;
    private readonly IData _dataClient;
    private readonly IPrefill _prefillService;
    private readonly IAppModel _appModel;
    private readonly IInstantiationProcessor _instantiationProcessor;
    private readonly IInstance _instanceClient;
    private readonly IEnumerable<IProcessTaskStart> _taskStarts;
    private readonly IEnumerable<IProcessTaskEnd> _taskEnds;
    private readonly IEnumerable<IProcessTaskAbandon> _taskAbandons;
    private readonly IPdfService _pdfService;
    private readonly IEFormidlingService? _eFormidlingService;
    private readonly AppSettings? _appSettings;
    private readonly LayoutEvaluatorStateInitializer _layoutEvaluatorStateInitializer;
    private readonly IFeatureManager _featureManager;

    /// <summary>
    /// Constructor with services from DI
    /// </summary>
    public DefaultTaskEvents(
        ILogger<DefaultTaskEvents> logger,
        IAppResources appResources,
        IAppMetadata appMetadata,
        IData dataClient,
        IPrefill prefillService,
        IAppModel appModel,
        IInstantiationProcessor instantiationProcessor,
        IInstance instanceClient,
        IEnumerable<IProcessTaskStart> taskStarts,
        IEnumerable<IProcessTaskEnd> taskEnds,
        IEnumerable<IProcessTaskAbandon> taskAbandons,
        IPdfService pdfService,
        IFeatureManager featureManager,
        LayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer,
        IOptions<AppSettings>? appSettings = null,
        IEFormidlingService? eFormidlingService = null
        )
    {
        _logger = logger;
        _appResources = appResources;
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _prefillService = prefillService;
        _appModel = appModel;
        _instantiationProcessor = instantiationProcessor;
        _instanceClient = instanceClient;
        _taskStarts = taskStarts;
        _taskEnds = taskEnds;
        _taskAbandons = taskAbandons;
        _pdfService = pdfService;
        _layoutEvaluatorStateInitializer = layoutEvaluatorStateInitializer;
        _eFormidlingService = eFormidlingService;
        _appSettings = appSettings?.Value;
        _featureManager = featureManager;
    }

    /// <inheritdoc />
    public async Task OnStartProcessTask(string taskId, Instance instance, Dictionary<string, string> prefill)
    {
        _logger.LogDebug("OnStartProcessTask for {InstanceId}", instance.Id);

        await RunAppDefinedOnTaskStart(taskId, instance, prefill);
        ApplicationMetadata? appMetadata = await _appMetadata.GetApplicationMetadata();

        // If this is a revisit to a previous task we need to unlock data
        foreach (DataType dataType in appMetadata.DataTypes.Where(dt => dt.TaskId == taskId))
        {
            DataElement? dataElement = instance.Data.Find(d => d.DataType == dataType.Id);

            if (dataElement != null && dataElement.Locked)
            {
                dataElement.Locked = false;
                _logger.LogDebug("Unlocking data element {DataElementId} of dataType {DataTypeId}", dataElement.Id, dataType.Id);
                await _dataClient.Update(instance, dataElement);
            }
        }

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

    private async Task RunAppDefinedOnTaskStart(string taskId, Instance instance, Dictionary<string, string> prefill)
    {
        foreach (var taskStart in _taskStarts)
        {
            await taskStart.Start(taskId, instance, prefill);
        }
    }

    /// <inheritdoc />
    public async Task OnEndProcessTask(string endEvent, Instance instance)
    {
        Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
        ApplicationMetadata? appMetadata = await _appMetadata.GetApplicationMetadata();
        List<DataType>? dataTypesToLock = appMetadata?.DataTypes.FindAll(dt => dt.TaskId == endEvent);

        await RunRemoveHiddenData(instance, instanceGuid, dataTypesToLock);

        await RunAppDefinedOnTaskEnd(endEvent, instance);

        await RunLockDataAndGeneratePdf(endEvent, instance, dataTypesToLock);

        await RunEformidling(endEvent, instance);

        await RunAutoDeleteOnProcessEnd(instance, instanceGuid);
    }

    private async Task RunRemoveHiddenData(Instance instance, Guid instanceGuid, List<DataType> dataTypesToLock)
    {
        if (_appSettings?.RemoveHiddenDataPreview == true)
        {
            await RemoveHiddenData(instance, instanceGuid, dataTypesToLock);
        }
    }

    private async Task RunAppDefinedOnTaskEnd(string endEvent, Instance instance)
    {
        foreach (var taskEnd in _taskEnds)
        {
            await taskEnd.End(endEvent, instance);
        }
    }

    private async Task RunLockDataAndGeneratePdf(string endEvent, Instance instance, List<DataType> dataTypesToLock)
    {
        _logger.LogDebug("OnEndProcessTask for {instanceId}. Locking data elements connected to {endEvent}", instance.Id, endEvent);

        foreach (DataType dataType in dataTypesToLock)
        {
            bool generatePdf = dataType.AppLogic?.ClassRef != null && dataType.EnablePdfCreation;

            foreach (DataElement dataElement in instance.Data.FindAll(de => de.DataType == dataType.Id))
            {
                dataElement.Locked = true;
                _logger.LogDebug("Locking data element {dataElementId} of dataType {dataTypeId}.", dataElement.Id, dataType.Id);
                Task updateData = _dataClient.Update(instance, dataElement);

                if (generatePdf)
                {
                    Task createPdf;
                    if (await _featureManager.IsEnabledAsync(FeatureFlags.NewPdfGeneration))
                    {
                        createPdf = _pdfService.GenerateAndStorePdf(instance, CancellationToken.None);
                    }
                    else
                    {
                        Type dataElementType = _appModel.GetModelType(dataType.AppLogic.ClassRef);
                        createPdf = _pdfService.GenerateAndStoreReceiptPDF(instance, endEvent, dataElement, dataElementType);
                    }

                    await Task.WhenAll(updateData, createPdf);
                }
                else
                {
                    await updateData;
                }
            }
        }
    }

    private async Task RunAutoDeleteOnProcessEnd(Instance instance, Guid instanceGuid)
    {
        ApplicationMetadata? appMetadata = await _appMetadata.GetApplicationMetadata();
        if (appMetadata != null && appMetadata.AutoDeleteOnProcessEnd && instance.Process?.Ended != null)
        {
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
            await _instanceClient.DeleteInstance(instanceOwnerPartyId, instanceGuid, true);
        }
    }

    private async Task RunEformidling(string endEvent, Instance instance)
    {
        ApplicationMetadata? appMetadata = await _appMetadata.GetApplicationMetadata();
        if (_appSettings?.EnableEFormidling == true && appMetadata?.EFormidling?.SendAfterTaskId == endEvent && _eFormidlingService != null)
        {
            // The code above updates data elements on the instance. To ensure
            // we have the latest instance with all the data elements including pdf,
            // we reload the instance before we pass it on to eFormidling.
            var updatedInstance = await _instanceClient.GetInstance(instance);
            await _eFormidlingService.SendEFormidlingShipment(updatedInstance);
        }
    }

    private async Task RemoveHiddenData(Instance instance, Guid instanceGuid, List<DataType> dataTypesToLock)
    {
        foreach (var dataType in dataTypesToLock.Where(dt => dt.AppLogic != null))
        {
            foreach (Guid dataElementId in instance.Data.Where(de => de.DataType == dataType.Id).Select(dataElement => Guid.Parse(dataElement.Id)))
            {
                // Delete hidden data in datamodel
                Type modelType = _appModel.GetModelType(dataType.AppLogic.ClassRef);
                string app = instance.AppId.Split("/")[1];
                int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
                object data = await _dataClient.GetFormData(
                    instanceGuid, modelType, instance.Org, app, instanceOwnerPartyId, dataElementId);

                if (_appSettings?.RemoveHiddenDataPreview == true)
                {
                    // Remove hidden data before validation
                    var layoutSet = _appResources.GetLayoutSetForTask(dataType.TaskId);
                    var evaluationState = await _layoutEvaluatorStateInitializer.Init(instance, data, layoutSet?.Id);
                    LayoutEvaluator.RemoveHiddenData(evaluationState);
                }

                // save the updated data if there are changes
                await _dataClient.InsertFormData(data, instanceGuid, modelType, instance.Org, app, instanceOwnerPartyId, dataType.Id);
            }
        }
    }

    /// <inheritdoc />
    public async Task OnAbandonProcessTask(string taskId, Instance instance)
    {
        foreach (var taskAbandon in _taskAbandons)
        {
            await taskAbandon.Abandon(taskId, instance);
        }

        _logger.LogDebug("OnAbandonProcessTask for {instanceId}. Locking data elements connected to {taskId}", instance.Id, taskId);
        await Task.CompletedTask;
    }

    private async Task UpdatePresentationTextsOnInstance(Instance instance, string dataType, dynamic data)
    {
        ApplicationMetadata? appMetadata = await _appMetadata.GetApplicationMetadata();
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
        ApplicationMetadata? appMetadata = await _appMetadata.GetApplicationMetadata();
        var updatedValues = DataHelper.GetUpdatedDataValues(
            appMetadata.DataFields,
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

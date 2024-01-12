using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Process.ProcessTasks;

/// <summary>
/// Contains common logic for ending a task
/// </summary>
public class ProcessTaskFinalizer : IProcessTaskFinalizer
{
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly IAppModel _appModel;
    private readonly IAppResources _appResources;
    private readonly LayoutEvaluatorStateInitializer _layoutEvaluatorStateInitializer;
    private readonly IOptions<AppSettings>? _appSettings;

    /// <summary>
    /// Contains common logic for ending a task
    /// </summary>
    public ProcessTaskFinalizer(IAppMetadata appMetadata,
        IDataClient dataClient,
        IAppModel appModel,
        IAppResources appResources,
        LayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer,
        IOptions<AppSettings>? appSettings = null)
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _appModel = appModel;
        _appResources = appResources;
        _layoutEvaluatorStateInitializer = layoutEvaluatorStateInitializer;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Runs common finalization logic for process tasks for a given task ID and instance. This method removes data elements generated from the task, removes hidden data and shadow fields.
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public async Task Finalize(string taskId, Instance instance)
    {
        Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        List<DataType> dataTypesToLock = applicationMetadata.DataTypes.FindAll(dt => dt.TaskId == taskId);

        await RemoveDataElementsGeneratedFromTask(instance, taskId);
        await RemoveHiddenData(instance, instanceGuid, dataTypesToLock);
        await RemoveShadowFields(instance, instanceGuid, dataTypesToLock);
    }

    public async Task RemoveDataElementsGeneratedFromTask(Instance instance, string endEvent)
    {
        AppIdentifier appIdentifier = new AppIdentifier(instance.AppId);
        InstanceIdentifier instanceIdentifier = new InstanceIdentifier(instance);
        foreach (var dataElement in instance.Data?.Where(de =>
                                        de.References != null &&
                                        de.References.Exists(r =>
                                            r.ValueType == ReferenceType.Task && r.Value == endEvent)) ??
                                    Enumerable.Empty<DataElement>())
        {
            await _dataClient.DeleteData(appIdentifier.Org, appIdentifier.App, instanceIdentifier.InstanceOwnerPartyId,
                instanceIdentifier.InstanceGuid, Guid.Parse(dataElement.Id), false);
        }
    }

    public async Task RemoveHiddenData(Instance instance, Guid instanceGuid, List<DataType>? connectedDataTypes)
    {
        if ((_appSettings?.Value?.RemoveHiddenData) != true)
        {
            return;
        }

        foreach (var dataType in connectedDataTypes?.Where(dt => dt.AppLogic != null) ?? Enumerable.Empty<DataType>())
        {
            foreach (Guid dataElementId in instance.Data.Where(de => de.DataType == dataType.Id)
                         .Select(dataElement => Guid.Parse(dataElement.Id)))
            {
                // Delete hidden data in datamodel
                Type modelType = _appModel.GetModelType(dataType.AppLogic.ClassRef);
                string app = instance.AppId.Split("/")[1];
                int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
                object data = await _dataClient.GetFormData(
                    instanceGuid, modelType, instance.Org, app, instanceOwnerPartyId, dataElementId);

                if (_appSettings?.Value?.RemoveHiddenData == true)
                {
                    // Remove hidden data before validation, ignore hidden rows. TODO: Determine how hidden rows should be handled going forward.
                    var layoutSet = _appResources.GetLayoutSetForTask(dataType.TaskId);
                    var evaluationState = await _layoutEvaluatorStateInitializer.Init(instance, data, layoutSet?.Id);
                    LayoutEvaluator.RemoveHiddenData(evaluationState, RowRemovalOption.Ignore);
                }

                // save the updated data if there are changes
                await _dataClient.UpdateData(data, instanceGuid, modelType, instance.Org, app, instanceOwnerPartyId,
                    dataElementId);
            }
        }
    }

    public async Task RemoveShadowFields(Instance instance, Guid instanceGuid, List<DataType> connectedDataTypes)
    {
        if (connectedDataTypes.Find(dt => dt.AppLogic?.ShadowFields?.Prefix != null) == null)
        {
            return;
        }

        foreach (var dataType in connectedDataTypes.Where(dt => dt.AppLogic?.ShadowFields != null))
        {
            foreach (Guid dataElementId in instance.Data.Where(de => de.DataType == dataType.Id)
                         .Select(dataElement => Guid.Parse(dataElement.Id)))
            {
                Type modelType = _appModel.GetModelType(dataType.AppLogic.ClassRef);
                string app = instance.AppId.Split("/")[1];
                int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
                dynamic data = await _dataClient.GetFormData(
                    instanceGuid, modelType, instance.Org, app, instanceOwnerPartyId, dataElementId);

                var modifier = new IgnorePropertiesWithPrefix(dataType.AppLogic.ShadowFields.Prefix);
                JsonSerializerOptions options = new()
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver
                    {
                        Modifiers = { modifier.ModifyPrefixInfo }
                    },
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                };

                string serializedData = JsonSerializer.Serialize(data, options);
                if (dataType.AppLogic.ShadowFields.SaveToDataType != null)
                {
                    var saveToDataType =
                        connectedDataTypes.Find(dt => dt.Id == dataType.AppLogic.ShadowFields.SaveToDataType);
                    if (saveToDataType == null)
                    {
                        throw new Exception(
                            $"SaveToDataType {dataType.AppLogic.ShadowFields.SaveToDataType} not found");
                    }

                    Type saveToModelType = _appModel.GetModelType(saveToDataType.AppLogic.ClassRef);
                    var updatedData = JsonSerializer.Deserialize(serializedData, saveToModelType);
                    await _dataClient.InsertFormData(updatedData, instanceGuid, saveToModelType ?? modelType,
                        instance.Org, app, instanceOwnerPartyId, saveToDataType.Id);
                }
                else
                {
                    var updatedData = JsonSerializer.Deserialize(serializedData, modelType);
                    await _dataClient.UpdateData(updatedData, instanceGuid, modelType, instance.Org, app,
                        instanceOwnerPartyId, dataElementId);
                }
            }
        }
    }
}
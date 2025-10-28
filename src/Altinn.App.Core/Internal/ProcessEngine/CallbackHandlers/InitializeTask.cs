using System.Globalization;
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Prefill;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.ProcessEngine.CallbackHandlers;

internal sealed class InitializeTaskHandler : IProcessEngineCallbackHandler
{
    private ILogger<InitializeTaskHandler> _logger;
    private IAppMetadata _appMetadata;
    private IPrefill _prefillService;
    private IAppModel _appModel;
    private AppImplementationFactory _appImplementationFactory;
    private IProcessTaskCleaner _processTaskCleaner;

    /// <summary>
    /// Gets the key for the callback handler.
    /// </summary>
    public string Key => "InitializeTask";

    public InitializeTaskHandler(ILogger<InitializeTaskHandler> logger,
        IAppMetadata appMetadata,
        IPrefill prefillService,
        IAppModel appModel,
        IServiceProvider serviceProvider,
        IProcessTaskCleaner processTaskCleaner
    )
    {
        _logger = logger;
        _appMetadata = appMetadata;
        _prefillService = prefillService;
        _appModel = appModel;
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
        _processTaskCleaner = processTaskCleaner;
    }

    public async Task<ProcessEngineCallbackHandlerResult> Execute(ProcessEngineCallbackHandlerParameters parameters)
    {
        await Initialize(
            parameters.InstanceDataMutator.Instance.Process.CurrentTask.ElementId,
            parameters.InstanceDataMutator.Instance,
            null
        );

        return new SuccessfulProcessEngineCallbackHandlerResult();
    }

    public async Task Initialize(IInstanceDataMutator instanceDataMutator, Dictionary<string, string>? prefill)
    {
        Instance instance = instanceDataMutator.Instance;
        string? taskId = instance.Process.CurrentTask.ElementId;

        _logger.LogDebug("OnStartProcessTask for {InstanceId}", instance.Id);

        await _processTaskCleaner.RemoveAllDataElementsGeneratedFromTask(instance, taskId);

        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

        foreach (
            DataType dataType in applicationMetadata.DataTypes.Where(dt =>
                dt.TaskId == taskId && dt.AppLogic?.AutoCreate == true
            )
        )
        {
            _logger.LogDebug("Auto create data element: {DataTypeId}", dataType.Id);

            DataElement? dataElement = instance.Data?.Find(d => d.DataType == dataType.Id);
            if (dataElement != null)
            {
                continue;
            }

            var data = _appModel.Create(dataType.AppLogic.ClassRef);

            // runs prefill from repo configuration if config exists
            await _prefillService.PrefillDataModel(instance.InstanceOwner.PartyId, dataType.Id, data, prefill);
            var instantiationProcessor = _appImplementationFactory.GetRequired<IInstantiationProcessor>();
            await instantiationProcessor.DataCreation(instance, data, prefill);

            Type type = _appModel.GetModelType(dataType.AppLogic.ClassRef);

            ObjectUtils.InitializeAltinnRowId(data);
            ObjectUtils.PrepareModelForXmlStorage(data);

            DataElement createdDataElement = await _dataClient.InsertFormData(instance, dataType.Id, data, type);
            instance.Data ??= [];
            instance.Data.Add(createdDataElement);

            await UpdatePresentationTextsOnInstance(instance, dataType.Id, data);
            await UpdateDataValuesOnInstance(instance, dataType.Id, data);

            _logger.LogDebug("Created data element: {CreatedDataElementId}", createdDataElement.Id);
        }
    }

    private async Task UpdatePresentationTextsOnInstance(Instance instance, string dataType, object data)
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        Dictionary<string, string?> updatedValues = DataHelper.GetUpdatedDataValues(
            applicationMetadata?.PresentationFields,
            instance.PresentationTexts,
            dataType,
            data
        );

        if (updatedValues.Count > 0)
        {
            Instance updatedInstance = await _instanceClient.UpdatePresentationTexts(
                int.Parse(instance.Id.Split("/")[0], CultureInfo.InvariantCulture),
                Guid.Parse(instance.Id.Split("/")[1]),
                new PresentationTexts { Texts = updatedValues }
            );

            instance.PresentationTexts = updatedInstance.PresentationTexts;
        }
    }

    private async Task UpdateDataValuesOnInstance(Instance instance, string dataType, object data)
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        Dictionary<string, string?> updatedValues = DataHelper.GetUpdatedDataValues(
            applicationMetadata?.DataFields,
            instance.DataValues,
            dataType,
            data
        );

        if (updatedValues.Count > 0)
        {
            Instance updatedInstance = await _instanceClient.UpdateDataValues(
                int.Parse(instance.Id.Split("/")[0], CultureInfo.InvariantCulture),
                Guid.Parse(instance.Id.Split("/")[1]),
                new DataValues { Values = updatedValues }
            );

            instance.DataValues = updatedInstance.DataValues;
        }
    }
}

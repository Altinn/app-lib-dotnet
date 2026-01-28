using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Prefill;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.TaskStart;

internal sealed class CommonTaskInitialization : IWorkflowEngineCommand
{
    public static string Key => "CommonTaskInitialization";

    public string GetKey() => Key;

    private IAppMetadata _appMetadata;
    private IPrefill _prefillService;
    private IAppModel _appModel;
    private AppImplementationFactory _appImplementationFactory;
    private IProcessTaskCleaner _processTaskCleaner;

    public CommonTaskInitialization(
        IAppMetadata appMetadata,
        IPrefill prefillService,
        IAppModel appModel,
        IServiceProvider serviceProvider,
        IProcessTaskCleaner processTaskCleaner
    )
    {
        _appMetadata = appMetadata;
        _prefillService = prefillService;
        _appModel = appModel;
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
        _processTaskCleaner = processTaskCleaner;
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        IInstanceDataMutator instanceDataMutator = parameters.InstanceDataMutator;
        Instance instance = instanceDataMutator.Instance;
        string taskId = instance.Process.CurrentTask.ElementId;

        await _processTaskCleaner.RemoveAllDataElementsGeneratedFromTask(instance, taskId);

        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

        foreach (
            DataType dataType in applicationMetadata.DataTypes.Where(dt =>
                dt.TaskId == taskId && dt.AppLogic?.AutoCreate == true
            )
        )
        {
            DataElement? dataElement = instance.Data?.Find(d => d.DataType == dataType.Id);
            if (dataElement != null)
            {
                continue;
            }

            object data = _appModel.Create(dataType.AppLogic.ClassRef);

            //TODO: How do we do prefill? Currently being set to empty array, which is not correct.
            await _prefillService.PrefillDataModel(instance.InstanceOwner.PartyId, dataType.Id, data, []);
            var instantiationProcessor = _appImplementationFactory.GetRequired<IInstantiationProcessor>();
            await instantiationProcessor.DataCreation(instance, data, []);

            instanceDataMutator.AddFormDataElement(dataType.Id, data);
        }

        return new SuccessfulProcessEngineCommandResult();
    }
}

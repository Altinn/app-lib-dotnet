using System.Diagnostics;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal sealed class ExecuteServiceTask(AppImplementationFactory appImplementationFactory, Telemetry? telemetry = null)
    : IProcessEngineCommand
{
    public static string Key => "ExecuteServiceTask";

    public string GetKey() => Key;

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        IInstanceDataMutator instanceDataMutator = parameters.InstanceDataMutator;
        Instance instance = parameters.InstanceDataMutator.Instance;
        string serviceTaskType = parameters.Payload.Metadata; //TODO: Define how to pass service task id

        using Activity? activity = telemetry?.StartProcessExecuteServiceTaskActivity(instance, serviceTaskType);

        try
        {
            ServiceTaskContext context = new()
            {
                InstanceDataMutator = instanceDataMutator,
                CancellationToken = parameters.CancellationToken,
            };

            IServiceTask serviceTask = GetServiceTask(serviceTaskType);
            ServiceTaskResult result = await serviceTask.Execute(context);

            if (result is ServiceTaskFailedResult)
            {
                return new FailedProcessEngineCommandResult(
                    new ProcessException($"Service task {serviceTask.Type} returned a failed result!")
                );
            }

            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            activity?.Errored(ex);
            return new FailedProcessEngineCommandResult(ex);
        }
    }

    private IServiceTask GetServiceTask(string type)
    {
        IEnumerable<IServiceTask> serviceTasks = appImplementationFactory.GetAll<IServiceTask>();
        IServiceTask? serviceTask = serviceTasks.FirstOrDefault(x =>
            x.Type.Equals(type, StringComparison.OrdinalIgnoreCase)
        );

        return serviceTask ?? throw new ProcessException($"No service task found for type {type}");
    }
}

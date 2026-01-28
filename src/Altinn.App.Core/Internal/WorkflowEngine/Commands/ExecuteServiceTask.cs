using System.Diagnostics;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Commands;

/// <summary>
/// Request payload for ExecuteServiceTask command.
/// Contains the service task type identifier.
/// </summary>
internal sealed record ExecuteServiceTaskPayload(string ServiceTaskType) : CommandRequestPayload;

internal sealed class ExecuteServiceTask(AppImplementationFactory appImplementationFactory, Telemetry? telemetry = null)
    : WorkflowEngineCommandBase<ExecuteServiceTaskPayload>
{
    public static string Key => "ExecuteServiceTask";

    public override string GetKey() => Key;

    public override async Task<ProcessEngineCommandResult> Execute(
        ProcessEngineCommandContext context,
        ExecuteServiceTaskPayload payload
    )
    {
        IInstanceDataMutator instanceDataMutator = context.InstanceDataMutator;
        Instance instance = context.InstanceDataMutator.Instance;
        string serviceTaskType = payload.ServiceTaskType;

        using Activity? activity = telemetry?.StartProcessExecuteServiceTaskActivity(instance, serviceTaskType);

        try
        {
            ServiceTaskContext serviceTaskContext = new()
            {
                InstanceDataMutator = instanceDataMutator,
                CancellationToken = context.CancellationToken,
            };

            IServiceTask serviceTask = GetServiceTask(serviceTaskType);
            ServiceTaskResult result = await serviceTask.Execute(serviceTaskContext);

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

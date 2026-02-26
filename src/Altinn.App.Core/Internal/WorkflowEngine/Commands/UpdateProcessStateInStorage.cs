using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Commands;

/// <summary>
/// Request payload for UpdateProcessState command.
/// Contains the complete process state change with old and new states.
/// </summary>
internal sealed record UpdateProcessStatePayload(ProcessStateChange ProcessStateChange) : CommandRequestPayload;

/// <summary>
/// Command that commits the process state transition to Storage.
/// This command persists the new process state and instance events after task transition logic has completed.
/// </summary>
internal sealed class UpdateProcessStateInStorage(IInstanceClient instanceClient)
    : WorkflowEngineCommandBase<UpdateProcessStatePayload>
{
    public static string Key => "UpdateProcessState";

    public override string GetKey() => Key;

    public override async Task<ProcessEngineCommandResult> Execute(
        ProcessEngineCommandContext context,
        UpdateProcessStatePayload payload
    )
    {
        try
        {
            ProcessStateChange processStateChange = payload.ProcessStateChange;

            if (processStateChange.NewProcessState == null)
            {
                return FailedProcessEngineCommandResult.Permanent(
                    "ProcessStateChange.NewProcessState is null",
                    "InvalidOperationException"
                );
            }

            Instance instance = context.InstanceDataMutator.Instance;
            instance.Process = processStateChange.NewProcessState;

            await instanceClient.UpdateProcessAndEvents(
                instance,
                processStateChange.Events ?? [],
                StorageAuthenticationMethod.ServiceOwner(),
                context.CancellationToken
            );

            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return FailedProcessEngineCommandResult.Retryable(ex);
        }
    }
}

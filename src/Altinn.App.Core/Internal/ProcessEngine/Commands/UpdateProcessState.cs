using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

/// <summary>
/// Request payload for UpdateProcessState command.
/// Contains the complete process state change with old and new states.
/// </summary>
internal sealed record UpdateProcessStatePayload(ProcessStateChange ProcessStateChange) : CommandRequestPayload;

/// <summary>
/// Command that commits the process state transition to Storage.
/// This command persists the new process state and instance events after task transition logic has completed.
/// </summary>
internal sealed class UpdateProcessState : ProcessEngineCommandBase<UpdateProcessStatePayload>
{
    public static string Key => "UpdateProcessState";

    public override string GetKey() => Key;

    public override Task<ProcessEngineCommandResult> Execute(
        ProcessEngineCommandContext context,
        UpdateProcessStatePayload payload
    )
    {
        try
        {
            ProcessStateChange processStateChange = payload.ProcessStateChange;

            if (processStateChange.NewProcessState == null)
            {
                return Task.FromResult<ProcessEngineCommandResult>(
                    new FailedProcessEngineCommandResult(
                        "ProcessStateChange.NewProcessState is null",
                        "InvalidOperationException"
                    )
                );
            }

            // Get the instance and update its process state
            Instance instance = context.InstanceDataMutator.Instance;
            instance.Process = processStateChange.NewProcessState;

            return Task.FromResult<ProcessEngineCommandResult>(new SuccessfulProcessEngineCommandResult());
        }
        catch (Exception ex)
        {
            return Task.FromResult<ProcessEngineCommandResult>(new FailedProcessEngineCommandResult(ex));
        }
    }
}

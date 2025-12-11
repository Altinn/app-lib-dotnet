using System.Text.Json;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

/// <summary>
/// Command that commits the process state transition to Storage.
/// This command persists the new process state and instance events after task transition logic has completed.
/// </summary>
internal sealed class UpdateProcessState : IProcessEngineCommand
{
    public static string Key => "UpdateProcessState";

    public string GetKey() => Key;

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext context)
    {
        try
        {
            // Deserialize the ProcessStateChange from the metadata
            if (string.IsNullOrEmpty(context.Payload.Metadata))
            {
                return new FailedProcessEngineCommandResult(
                    new InvalidOperationException(
                        "ProcessStateChange metadata is required for UpdateProcessState command"
                    )
                );
            }

            ProcessStateChange? processStateChange = JsonSerializer.Deserialize<ProcessStateChange>(
                context.Payload.Metadata
            );

            if (processStateChange?.NewProcessState == null)
            {
                return new FailedProcessEngineCommandResult(
                    new InvalidOperationException("ProcessStateChange.NewProcessState is null")
                );
            }

            // Get the instance and update its process state
            Instance instance = context.InstanceDataMutator.Instance;
            instance.Process = processStateChange.NewProcessState;

            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }
    }
}

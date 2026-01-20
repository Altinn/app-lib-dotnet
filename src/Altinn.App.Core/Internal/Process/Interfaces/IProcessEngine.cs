using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Process engine interface that defines the Altinn App process engine
/// </summary>
internal interface IProcessEngine
{
    /// <summary>
    /// Generates process start events and updates the instance's process state in memory.
    /// Does not persist anything - use <see cref="SubmitInitialProcessState"/> to dispatch to the async engine.
    /// </summary>
    Task<ProcessChangeResult> CreateInitialProcessState(ProcessStartRequest request);

    /// <summary>
    /// Dispatches a process state change to the async process engine and waits for completion.
    /// </summary>
    Task SubmitInitialProcessState(
        Instance instance,
        ProcessStateChange processStateChange,
        CancellationToken ct = default
    );

    /// <summary>
    /// Method to move process to next task/event
    /// </summary>
    Task<ProcessChangeResult> Next(ProcessNextRequest request, CancellationToken ct = default);
}

using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Models.Process;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Process engine interface that defines the Altinn App process engine
/// </summary>
internal interface IProcessEngine
{
    /// <summary>
    /// Starts a new process for an instance
    /// </summary>
    Task<ProcessChangeResult> Start(ProcessStartRequest request, CancellationToken ct = default);

    /// <summary>
    /// Method to move process to next task/event
    /// </summary>
    Task<ProcessChangeResult> Next(ProcessNextRequest request, CancellationToken ct = default);
}

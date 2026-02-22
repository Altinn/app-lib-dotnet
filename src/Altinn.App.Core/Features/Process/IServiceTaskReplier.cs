using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Process;

/// <summary>
/// Interface for to give a reply for an IReplyServiceTask.
/// </summary>
public interface IServiceTaskReplier
{
    /// <summary>
    /// Sends the reply.
    /// </summary>
    Task SendReply(string correlationId, string payload, CancellationToken cancellationToken = default);
}

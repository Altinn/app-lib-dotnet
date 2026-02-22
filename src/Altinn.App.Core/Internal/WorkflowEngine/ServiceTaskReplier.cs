using Altinn.App.Core.Features.Process;
using Altinn.App.Core.Internal.WorkflowEngine.Http;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine;

internal class ServiceTaskReplier(IWorkflowEngineClient client) : IServiceTaskReplier
{
    public Task SendReply(string correlationId, string payload, CancellationToken cancellationToken = default)
    {
        return client.SendReply(correlationId, payload, cancellationToken);
    }
}

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

internal sealed record ServiceTaskType(string Name, ServiceTaskKind Kind);

internal enum ServiceTaskKind
{
    ServiceTask,
    ReplyServiceTask,
}

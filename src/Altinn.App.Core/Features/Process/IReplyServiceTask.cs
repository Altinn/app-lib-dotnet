namespace Altinn.App.Core.Features.Process;

/// <summary>
/// Interface for service tasks that require an asynchronous reply from an external system.
/// </summary>
/// <remarks>
/// <para>
/// <strong>IMPORTANT: Implementations MUST be idempotent - service tasks may be retried on failure.</strong>
/// </para>
/// </remarks>
[ImplementableByApps]
public interface IReplyServiceTask : IServiceTask
{
    /// <summary>
    /// Processes the reply received from an external system.
    /// </summary>
    /// <param name="context">The service task context, including <see cref="ServiceTaskContext.CorrelationId"/> and <see cref="ServiceTaskContext.InstanceDataMutator"/>.</param>
    /// <param name="payload">The reply payload submitted by the external party.</param>
    public Task<ServiceTaskResult> ProcessReply(ServiceTaskContext context, string payload);
}

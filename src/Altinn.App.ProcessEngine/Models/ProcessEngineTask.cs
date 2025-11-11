using System.Diagnostics.CodeAnalysis;

namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineTask : ProcessEngineDatabaseItem
{
    public required int ProcessingOrder { get; init; }
    public required ProcessEngineCommand Command { get; init; }
    public required InstanceInformation InstanceInformation { get; init; }
    public required ProcessEngineActor ProcessEngineActor { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? BackoffUntil { get; set; }
    public ProcessEngineRetryStrategy? RetryStrategy { get; init; }
    public int RequeueCount { get; set; }

    // TODO: Find a better name for this
    public Task<ProcessEngineExecutionResult>? ExecutionTask { get; set; }

    [MemberNotNullWhen(true, nameof(ExecutionTask))]
    public bool IsExecuting => ExecutionTask is not null;

    public static ProcessEngineTask FromRequest(
        ProcessEngineCommandRequest request,
        ProcessEngineActor processEngineActor,
        int index
    ) =>
        new()
        {
            Identifier = $"{request.InstanceInformation.InstanceGuid}/{request.Command}",
            CreatedAt = request.CreatedAt ?? DateTimeOffset.UtcNow, // TODO: Hmm...
            ProcessEngineActor = processEngineActor,
            StartTime = request.StartTime,
            ProcessingOrder = index,
            Command = request.Command,
            RetryStrategy = request.RetryStrategy,
            InstanceInformation = request.InstanceInformation,
        };

    public override string ToString() => $"{nameof(ProcessEngineTask)}.{Command.GetType()}: {Identifier} ({Status})";

    public bool Equals(ProcessEngineTask? other) =>
        other?.Identifier.Equals(Identifier, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Identifier.GetHashCode();

    public new void Dispose()
    {
        ExecutionTask?.Dispose();
        DatabaseTask?.Dispose();
    }
}

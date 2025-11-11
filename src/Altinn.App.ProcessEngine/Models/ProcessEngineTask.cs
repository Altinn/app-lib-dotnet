namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineTask : ProcessEngineDatabaseItem
{
    public required int ProcessingOrder { get; init; }
    public required ProcessEngineCommand Command { get; init; }
    public required ProcessEngineActor ProcessEngineActor { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? BackoffUntil { get; set; }
    public ProcessEngineRetryStrategy? RetryStrategy { get; init; }
    public int RequeueCount { get; set; }
    public Task<ProcessEngineExecutionResult>? ExecutionTask { get; set; }

    public static ProcessEngineTask FromRequest(
        string jobIdentifier,
        ProcessEngineCommandRequest request,
        ProcessEngineActor processEngineActor,
        int index
    ) =>
        new()
        {
            Identifier = $"{jobIdentifier}/{request.Command}",
            CreatedAt = request.CreatedAt ?? DateTimeOffset.UtcNow, // TODO: Hmm...
            ProcessEngineActor = processEngineActor,
            StartTime = request.StartTime,
            ProcessingOrder = index,
            Command = request.Command,
            RetryStrategy = request.RetryStrategy,
        };

    public override string ToString() =>
        $"[{nameof(ProcessEngineTask)}.{Command.GetType().Name}] {Identifier} ({Status})";

    public new void Dispose()
    {
        ExecutionTask?.Dispose();
        DatabaseTask?.Dispose();
    }
}

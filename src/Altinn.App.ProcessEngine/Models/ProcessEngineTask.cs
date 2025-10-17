namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineTask
{
    public string Identifier => Command.Identifier;
    public ProcessEngineItemStatus Status { get; set; }
    public required int ProcessingOrder { get; init; }
    public required ProcessEngineCommand Command { get; init; }
    public required InstanceInformation InstanceInformation { get; init; }
    public required ProcessEngineActor ProcessEngineActor { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? LastUpdate { get; set; }
    public DateTimeOffset? BackoffUntil { get; set; }
    public ProcessEngineRetryStrategy? RetryStrategy { get; init; }
    public int RequeueCount { get; set; }
    public Task<ProcessEngineExecutionResult>? ExecutionTask { get; set; }

    public static ProcessEngineTask FromRequest(
        ProcessEngineCommandRequest request,
        ProcessEngineActor processEngineActor,
        int index
    ) =>
        new()
        {
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
}

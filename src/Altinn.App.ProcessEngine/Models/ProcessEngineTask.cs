namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineTask
{
    public ProcessEngineItemStatus Status { get; set; }
    public required string Identifier { get; init; }
    public required int ProcessingOrder { get; init; }
    public required ProcessEngineTaskCommand Command { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? LastUpdate { get; set; }
    public DateTimeOffset? BackoffUntil { get; set; }
    public ProcessEngineRetryStrategy? RetryStrategy { get; init; }
    public int RequeueCount { get; set; }
    public Task<ProcessEngineExecutionResult>? ExecutionTask { get; set; }

    public static ProcessEngineTask FromRequest(ProcessEngineTaskRequest request, int index) =>
        new()
        {
            Identifier = request.Identifier,
            StartTime = request.StartTime,
            ProcessingOrder = index,
            Command = request.Command,
        };

    public override string ToString() => $"{nameof(ProcessEngineTask)}.{Command.GetType()}: {Identifier}";

    public bool Equals(ProcessEngineTask? other) =>
        other?.Identifier.Equals(Identifier, StringComparison.OrdinalIgnoreCase) is true;

    public override int GetHashCode() => Identifier.GetHashCode();
};

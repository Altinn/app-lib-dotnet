namespace Altinn.App.ProcessEngine.Models;

public abstract record ProcessEngineTaskCommand
{
    public ProcessEngineTaskExecutionStrategy ExecutionStrategy { get; init; }
    public TimeSpan? MaxExecutionTime { get; init; }

    private ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy executionStrategy, TimeSpan? time = null)
    {
        ExecutionStrategy = executionStrategy;
        MaxExecutionTime = time;
    }

    // TODO: These instructions are just placeholders, and don't particularly make any sense right now

    public sealed record MoveProcessForward(string From, string To, string? Action = null)
        : ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy.WaitForCompletion);

    public sealed record ExecuteServiceTask(string Identifier)
        : ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy.PeriodicPolling);

    public sealed record ExecuteInterfaceHooks(object Something)
        : ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy.PeriodicPolling);

    public sealed record SendCorrespondence(object Something)
        : ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy.PeriodicPolling);

    public sealed record SendEformidling(object Something)
        : ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy.PeriodicPolling);

    public sealed record SendFiksArkiv(object Something)
        : ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy.PeriodicPolling);

    public sealed record PublishAltinnEvent(object Something)
        : ProcessEngineTaskCommand(ProcessEngineTaskExecutionStrategy.WaitForCompletion);
}

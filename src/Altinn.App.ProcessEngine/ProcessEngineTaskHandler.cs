using Microsoft.Extensions.Options;

namespace Altinn.App.ProcessEngine;

internal sealed record ProcessEngineExecutionResult(ProcessEngineExecutionStatus Status, string? Message = null);

internal enum ProcessEngineExecutionStatus
{
    Success,
    Error,
}

internal static class ProcessEngineExecutionStatusExtensions
{
    public static bool IsSuccess(this ProcessEngineExecutionResult result) =>
        result.Status == ProcessEngineExecutionStatus.Success;

    public static bool IsError(this ProcessEngineExecutionResult result) =>
        result.Status == ProcessEngineExecutionStatus.Error;
}

internal interface IProcessEngineTaskHandler
{
    Task<ProcessEngineExecutionResult> Execute(ProcessEngineTask task, CancellationToken cancellationToken);
}

internal class ProcessEngineTaskHandler : IProcessEngineTaskHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ProcessEngineSettings _settings;

    public ProcessEngineTaskHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        _settings = serviceProvider.GetRequiredService<IOptions<ProcessEngineSettings>>().Value;
    }

    public async Task<ProcessEngineExecutionResult> Execute(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        return task.Instruction switch
        {
            ProcessEngineTaskInstruction.MoveProcessForward => await MoveProcessForward(task, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown instruction: {task.Instruction}"),
        };
    }

    private Task<ProcessEngineExecutionResult> MoveProcessForward(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }
}

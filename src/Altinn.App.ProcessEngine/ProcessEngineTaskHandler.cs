using Altinn.App.ProcessEngine.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.ProcessEngine;

internal interface IProcessEngineTaskHandler
{
    Task<ProcessEngineExecutionResult> Execute(ProcessEngineTask task, CancellationToken cancellationToken);
}

internal class ProcessEngineTaskHandler : IProcessEngineTaskHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ProcessEngineSettings _settings;
    private readonly ILogger<ProcessEngineTaskHandler> _logger;

    public ProcessEngineTaskHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        _settings = serviceProvider.GetRequiredService<IOptions<ProcessEngineSettings>>().Value;
        _logger = serviceProvider.GetRequiredService<ILogger<ProcessEngineTaskHandler>>();
    }

    public async Task<ProcessEngineExecutionResult> Execute(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        return task.Instruction switch
        {
            ProcessEngineTaskInstruction.MoveProcessForward => await MoveProcessForward(task, cancellationToken),
            ProcessEngineTaskInstruction.ExecuteServiceTask => await ExecuteServiceTask(task, cancellationToken),
            ProcessEngineTaskInstruction.ExecuteInterfaceHooks => await ExecuteInterfaceHooks(task, cancellationToken),
            ProcessEngineTaskInstruction.SendCorrespondence => await SendCorrespondence(task, cancellationToken),
            ProcessEngineTaskInstruction.SendEformidling => await SendEformidling(task, cancellationToken),
            ProcessEngineTaskInstruction.SendFiksArkiv => await SendFiksArkiv(task, cancellationToken),
            ProcessEngineTaskInstruction.PublishAltinnEvent => await PublishAltinnEvent(task, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown instruction: {task.Instruction}"),
        };
    }

    private ValueTask<ProcessEngineExecutionResult> PublishAltinnEvent(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private ValueTask<ProcessEngineExecutionResult> SendFiksArkiv(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private ValueTask<ProcessEngineExecutionResult> SendEformidling(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private ValueTask<ProcessEngineExecutionResult> SendCorrespondence(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private ValueTask<ProcessEngineExecutionResult> ExecuteInterfaceHooks(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private ValueTask<ProcessEngineExecutionResult> ExecuteServiceTask(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private ValueTask<ProcessEngineExecutionResult> MoveProcessForward(
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }
}

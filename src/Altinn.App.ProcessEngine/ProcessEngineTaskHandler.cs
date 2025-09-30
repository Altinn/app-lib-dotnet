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
    private readonly IHttpClientFactory _httpClientFactory;

    public ProcessEngineTaskHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        _settings = serviceProvider.GetRequiredService<IOptions<ProcessEngineSettings>>().Value;
        _logger = serviceProvider.GetRequiredService<ILogger<ProcessEngineTaskHandler>>();
    }

    public async Task<ProcessEngineExecutionResult> Execute(ProcessEngineTask task, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(task.Command.MaxExecutionTime ?? _settings.DefaultTaskExecutionTimeout);

        try
        {
            return task.Command switch
            {
                ProcessEngineTaskCommand.MoveProcessForward => await MoveProcessForward(task, cts.Token),
                ProcessEngineTaskCommand.ExecuteServiceTask => await ExecuteServiceTask(task, cts.Token),
                ProcessEngineTaskCommand.ExecuteInterfaceHooks => await ExecuteInterfaceHooks(task, cts.Token),
                ProcessEngineTaskCommand.SendCorrespondence => await SendCorrespondence(task, cts.Token),
                ProcessEngineTaskCommand.SendEformidling => await SendEformidling(task, cts.Token),
                ProcessEngineTaskCommand.SendFiksArkiv => await SendFiksArkiv(task, cts.Token),
                ProcessEngineTaskCommand.PublishAltinnEvent => await PublishAltinnEvent(task, cts.Token),
                _ => throw new InvalidOperationException($"Unknown instruction: {task.Command}"),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing task {Task}: {Message}", task, e.Message);
            return ProcessEngineExecutionResult.Error(e.Message);
        }
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

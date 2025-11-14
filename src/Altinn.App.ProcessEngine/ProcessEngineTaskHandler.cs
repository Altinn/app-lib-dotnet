using Altinn.App.ProcessEngine.Constants;
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
                ProcessEngineCommand.AppCommand cmd => await AppCommand(cmd, task, cts.Token),
                _ => throw new InvalidOperationException($"Unknown instruction: {task.Command}"),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing task {Task}: {Message}", task, e.Message);
            return ProcessEngineExecutionResult.Error(e.Message);
        }
    }

    private async Task<ProcessEngineExecutionResult> AppCommand(
        ProcessEngineCommand.AppCommand command,
        ProcessEngineTask task,
        CancellationToken cancellationToken
    )
    {
        using var httpClient = GetAuthorizedAppClient(task.InstanceInformation);
        httpClient.Timeout = command.MaxExecutionTime ?? _settings.DefaultTaskExecutionTimeout;

        var payload = new ProcessEngineCallbackPayload(task.ProcessEngineActor, command.Metadata);
        using var response = await httpClient.PostAsync(
            command.CommandKey,
            JsonContent.Create(payload),
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            return ProcessEngineExecutionResult.Success();
        }

        // Extract error information from response body
        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<ProcessEngineCallbackErrorResponse>(
                cancellationToken
            );
            var errorMessage = errorResponse?.Message ?? $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            return ProcessEngineExecutionResult.Error(errorMessage);
        }
        catch
        {
            // Fallback if we can't parse the error response
            return ProcessEngineExecutionResult.Error($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
    }

    private HttpClient GetAuthorizedAppClient(InstanceInformation instanceInformation)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthConstants.ApiKeyHeaderName, _settings.ApiKey);

        // TODO: Fix this! Needs a way to resolve the correct address for the app
        client.BaseAddress = new Uri(
            $"http://local.altinn.cloud/{instanceInformation.Org}/{instanceInformation.App}/instances/{instanceInformation.InstanceOwnerPartyId}/{instanceInformation.InstanceGuid}/process-engine-callbacks/"
        );

        return client;
    }
}

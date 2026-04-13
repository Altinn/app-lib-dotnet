using System.Net;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.WorkflowEngine.Models.Engine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.WorkflowEngine.Http;

/// <summary>
/// HTTP client for communicating with the workflow engine service.
/// </summary>
internal sealed class WorkflowEngineClient : IWorkflowEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly PlatformSettings _platformSettings;
    private readonly ILogger<WorkflowEngineClient> _logger;

    public WorkflowEngineClient(
        HttpClient httpClient,
        IOptions<PlatformSettings> platformSettings,
        ILogger<WorkflowEngineClient> logger
    )
    {
        _httpClient = httpClient;
        _platformSettings = platformSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkflowEnqueueResponse.Accepted> EnqueueWorkflows(
        WorkflowEnqueueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        string url = $"{GetWorkflowEngineEndpoint()}/workflows";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = JsonContent.Create(request);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Workflow engine enqueue failed with status {StatusCode}. URL: {Url}. Response body: {Body}",
                response.StatusCode,
                url,
                body
            );
        }
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<WorkflowEnqueueResponse.Accepted>(
                cancellationToken: cancellationToken
            ) ?? throw new Exception("The expected workflow enqueue response was not found in the response content.");
    }

    /// <inheritdoc />
    public async Task<WorkflowStatusResponse?> GetWorkflow(
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{GetWorkflowEngineEndpoint()}/workflows/{workflowId}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return await response.Content.ReadFromJsonAsync<WorkflowStatusResponse>(
                    cancellationToken: cancellationToken
                ) ?? throw new Exception("The expected workflow status was not found in the response content.");
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorkflowStatusResponse>> ListActiveWorkflows(
        string ns,
        Guid? correlationId = null,
        Dictionary<string, string>? labels = null,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{GetWorkflowEngineEndpoint()}/workflows/?namespace={Uri.EscapeDataString(ns)}";
        if (correlationId.HasValue)
        {
            url += $"&correlationId={correlationId.Value}";
        }
        if (labels is not null)
        {
            foreach (var (key, value) in labels)
            {
                url += $"&label={Uri.EscapeDataString(key)}:{Uri.EscapeDataString(value)}";
            }
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<WorkflowStatusResponse>>(
                cancellationToken: cancellationToken
            ) ?? [];
    }

    /// <inheritdoc />
    public async Task<CancelWorkflowResponse> CancelWorkflow(
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"{GetWorkflowEngineEndpoint()}/workflows/{workflowId}/cancel";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CancelWorkflowResponse>(cancellationToken: cancellationToken)
            ?? throw new Exception("The expected cancel workflow response was not found in the response content.");
    }

    private string GetWorkflowEngineEndpoint() => _platformSettings.ApiWorkflowEngineEndpoint.TrimEnd('/');
}

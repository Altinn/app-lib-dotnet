using System.Net;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.WorkflowEngine.Http;

/// <summary>
/// HTTP client for communicating with the workflow engine service.
/// </summary>
internal sealed class WorkflowEngineClient : IWorkflowEngineClient
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly HttpClient _httpClient;
    private readonly AppIdentifier _appIdentifier;
    private readonly PlatformSettings _platformSettings;
    private readonly ILogger<WorkflowEngineClient> _logger;

    public WorkflowEngineClient(
        AppIdentifier appIdentifier,
        HttpClient httpClient,
        IOptions<PlatformSettings> platformSettings,
        ILogger<WorkflowEngineClient> logger
    )
    {
        _appIdentifier = appIdentifier;
        _httpClient = httpClient;
        _platformSettings = platformSettings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkflowEnqueueResponse.Accepted> EnqueueWorkflow(
        Instance instance,
        WorkflowEnqueueRequest request,
        CancellationToken cancellationToken = default
    )
    {
        string url = $"{GetBaseUrl()}{GetInstancePath(instance)}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = JsonContent.Create(request);
        httpRequest.Headers.Add(ApiKeyHeaderName, _platformSettings.WorkflowEngineApiKey);

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
    public async Task<WorkflowStatusResponse?> GetWorkflowStatus(
        Instance instance,
        Guid workflowId,
        CancellationToken cancellationToken = default
    )
    {
        string url = $"{GetBaseUrl()}{GetInstancePath(instance)}/{workflowId}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add(ApiKeyHeaderName, _platformSettings.WorkflowEngineApiKey);

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
        Instance instance,
        CancellationToken cancellationToken = default
    )
    {
        string url = $"{GetBaseUrl()}{GetInstancePath(instance)}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add(ApiKeyHeaderName, _platformSettings.WorkflowEngineApiKey);

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

    private string GetBaseUrl()
    {
        string baseUrl = _platformSettings.ApiWorkflowEngineEndpoint.TrimEnd('/');
        return $"{baseUrl}/{_appIdentifier.Org}/{_appIdentifier.App}/";
    }

    private static string GetInstancePath(Instance instance)
    {
        var instanceIdentifier = new InstanceIdentifier(instance);
        return $"{instanceIdentifier.InstanceOwnerPartyId}/{instanceIdentifier.InstanceGuid}";
    }
}

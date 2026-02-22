using System.Net;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
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

    public WorkflowEngineClient(
        AppIdentifier appIdentifier,
        HttpClient httpClient,
        IOptions<PlatformSettings> platformSettings
    )
    {
        _appIdentifier = appIdentifier;
        _httpClient = httpClient;
        _platformSettings = platformSettings.Value;
    }

    /// <inheritdoc />
    public async Task ProcessNext(
        Instance instance,
        ProcessNextRequest request,
        CancellationToken cancellationToken = default
    )
    {
        string url = $"{GetBaseUrl()}{GetInstancePath(instance)}/next";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = JsonContent.Create(request);
        httpRequest.Headers.Add(ApiKeyHeaderName, _platformSettings.WorkflowEngineApiKey);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<WorkflowStatusResponse?> GetActiveJobStatus(
        Instance instance,
        CancellationToken cancellationToken = default
    )
    {
        string url = $"{GetBaseUrl()}{GetInstancePath(instance)}/status";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add(ApiKeyHeaderName, _platformSettings.WorkflowEngineApiKey);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return await response.Content.ReadFromJsonAsync<WorkflowStatusResponse>(
                    cancellationToken: cancellationToken
                ) ?? throw new Exception("The expected process engine status was not found in the response content.");
        }

        return null;
    }

    public async Task SendReply(string correlationId, string payload, CancellationToken cancellationToken = default)
    {
        string url = $"{GetBaseUrl()}reply/{correlationId}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = JsonContent.Create(new { payload });
        httpRequest.Headers.Add(ApiKeyHeaderName, _platformSettings.WorkflowEngineApiKey);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
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

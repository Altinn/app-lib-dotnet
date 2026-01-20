using System.Net;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.ProcessEngine.Http;

/// <summary>
/// HTTP client for communicating with the Process Engine service.
/// Sends ProcessNextRequest to the process engine endpoint to enqueue jobs.
/// </summary>
internal sealed class ProcessEngineClient : IProcessEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly AppIdentifier _appIdentifier;
    private readonly GeneralSettings _generalSettings;
    private readonly ProcessEngineSettings _processEngineSettings;

    public ProcessEngineClient(
        AppIdentifier appIdentifier,
        HttpClient httpClient,
        IOptions<GeneralSettings> generalSettings,
        IOptions<ProcessEngineSettings> processEngineSettings
    )
    {
        _appIdentifier = appIdentifier;
        _httpClient = httpClient;
        _generalSettings = generalSettings.Value;
        _processEngineSettings = processEngineSettings.Value;
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
        httpRequest.Headers.Add(AuthConstants.ApiKeyHeaderName, _processEngineSettings.ApiKey);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ProcessEngineStatusResponse?> GetActiveJobStatus(
        Instance instance,
        CancellationToken cancellationToken = default
    )
    {
        string url = $"{GetBaseUrl()}{GetInstancePath(instance)}/status";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Add(AuthConstants.ApiKeyHeaderName, _processEngineSettings.ApiKey);

        HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return await response.Content.ReadFromJsonAsync<ProcessEngineStatusResponse>(
                    cancellationToken: cancellationToken
                ) ?? throw new Exception("The expected process engine status was not found in the response content.");
        }

        return null;
    }

    private string GetBaseUrl()
    {
        return $"http://{_generalSettings.HostName}/process-engine/{_appIdentifier.Org}/{_appIdentifier.App}/";
    }

    private static string GetInstancePath(Instance instance)
    {
        var instanceIdentifier = new InstanceIdentifier(instance);
        return $"{instanceIdentifier.InstanceOwnerPartyId}/{instanceIdentifier.InstanceGuid}";
    }
}

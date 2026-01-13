using System.Net;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
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

    public ProcessEngineClient(
        IOptions<GeneralSettings> generalSettings,
        AppIdentifier appIdentifier,
        HttpClient httpClient
    )
    {
        string baseUrl = generalSettings.Value.FormattedExternalAppBaseUrl(appIdentifier);
        httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task ProcessNext(
        Instance instance,
        ProcessNextRequest request,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"{CreateInstanceUrl(instance)}/process-engine/next",
            request,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    public async Task<ProcessEngineStatusResponse?> GetActiveJobStatus(
        Instance instance,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{CreateInstanceUrl(instance)}/process-engine/status",
            cancellationToken
        );

        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return await response.Content.ReadFromJsonAsync<ProcessEngineStatusResponse>(
                    cancellationToken: cancellationToken
                ) ?? throw new Exception("The expected process engine status was not found in the response content.");
        }

        return null;
    }

    private static string CreateInstanceUrl(Instance instance)
    {
        var instanceIdentifier = new InstanceIdentifier(instance);
        return $"{instanceIdentifier.InstanceOwnerPartyId}/{instanceIdentifier.InstanceGuid}";
    }
}

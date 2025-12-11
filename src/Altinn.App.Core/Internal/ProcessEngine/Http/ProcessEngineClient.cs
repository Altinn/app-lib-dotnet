using System.Net.Http.Json;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

/// <summary>
/// HTTP client for communicating with the Process Engine service.
/// Sends ProcessNextRequest to the process engine endpoint to enqueue jobs.
/// </summary>
internal sealed class ProcessEngineClient : IProcessEngineClient
{
    private readonly HttpClient _httpClient;

    public ProcessEngineClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task ProcessNext(ProcessNextRequest request, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "/process-engine/next",
            request,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Process.ProcessLock;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.Storage;

internal sealed class ProcessLockClient
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<ProcessLockClient> _logger;
    private readonly HttpClient _client;
    private readonly Telemetry? _telemetry;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProcessLockClient(
        IOptions<PlatformSettings> platformSettings,
        IOptions<AppSettings> appSettings,
        ILogger<ProcessLockClient> logger,
        IHttpContextAccessor httpContextAccessor,
        HttpClient httpClient,
        Telemetry? telemetry = null
    )
    {
        _appSettings = appSettings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        httpClient.BaseAddress = new Uri(platformSettings.Value.ApiStorageEndpoint);
        httpClient.DefaultRequestHeaders.Add(General.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client = httpClient;
        _telemetry = telemetry;
    }

    public async Task<Guid> AcquireProcessLock(Guid instanceGuid, int instanceOwnerPartyId, TimeSpan expiration)
    {
        using var activity = _telemetry?.StartAcquireProcessLockActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/process/lock";
        string token = JwtTokenUtil.GetTokenFromContext(
            _httpContextAccessor.HttpContext,
            _appSettings.RuntimeCookieName
        );

        var request = new ProcessLockRequest { Expiration = (int)expiration.TotalSeconds };
        var content = JsonContent.Create(request);

        using var response = await _client.PostAsync(token, apiUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw await PlatformHttpResponseSnapshotException.CreateAndDisposeHttpResponse(response);
        }

        Guid? lockId = null;
        try
        {
            var lockResponse = await response.Content.ReadFromJsonAsync<ProcessLockResponse>();
            lockId = lockResponse?.LockId;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error reading response from the lock acquisition endpoint.");
        }

        if (lockId is null || lockId.Value == Guid.Empty)
        {
            throw PlatformHttpResponseSnapshotException.Create(
                "The response from the lock acquisition endpoint was not expected.",
                response
            );
        }

        return lockId.Value;
    }

    public async Task ReleaseProcessLock(Guid instanceGuid, int instanceOwnerPartyId, Guid lockId)
    {
        using var activity = _telemetry?.StartReleaseProcessLockActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/process/lock/{lockId}";
        string token = JwtTokenUtil.GetTokenFromContext(
            _httpContextAccessor.HttpContext,
            _appSettings.RuntimeCookieName
        );

        var request = new ProcessLockRequest { Expiration = 0 };
        var content = JsonContent.Create(request);

        using var response = await _client.PatchAsync(token, apiUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw await PlatformHttpResponseSnapshotException.CreateAndDisposeHttpResponse(response);
        }
    }
}

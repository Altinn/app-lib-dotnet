using System.Net.Http.Headers;
using System.Net.Http.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.Platform.Storage.Interface.Models;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.Storage;

internal sealed class InstanceLockClient
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<InstanceLockClient> _logger;
    private readonly HttpClient _client;
    private readonly Telemetry? _telemetry;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InstanceLockClient(
        IOptions<PlatformSettings> platformSettings,
        IOptions<AppSettings> appSettings,
        ILogger<InstanceLockClient> logger,
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

    public async Task<string> AcquireInstanceLock(Guid instanceGuid, int instanceOwnerPartyId, TimeSpan expiration)
    {
        using var activity = _telemetry?.StartAcquireInstanceLockActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/lock";
        string token = JwtTokenUtil.GetTokenFromContext(
            _httpContextAccessor.HttpContext,
            _appSettings.RuntimeCookieName
        );

        var request = new InstanceLockRequest { TtlSeconds = (int)expiration.TotalSeconds };
        var content = JsonContent.Create(request);

        using var response = await _client.PostAsync(token, apiUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw await PlatformHttpResponseSnapshotException.CreateAndDisposeHttpResponse(response);
        }

        string? lockToken = null;
        try
        {
            var lockResponse = await response.Content.ReadFromJsonAsync<InstanceLockResponse>();
            lockToken = lockResponse?.LockToken;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error reading response from the lock acquisition endpoint.");
        }

        if (string.IsNullOrEmpty(lockToken))
        {
            throw PlatformHttpResponseSnapshotException.Create(
                "The response from the lock acquisition endpoint was not expected.",
                response
            );
        }

        return lockToken;
    }

    public async Task ReleaseInstanceLock(Guid instanceGuid, int instanceOwnerPartyId, string lockToken)
    {
        using var activity = _telemetry?.StartReleaseInstanceLockActivity(instanceGuid, instanceOwnerPartyId);
        string apiUrl = $"instances/{instanceOwnerPartyId}/{instanceGuid}/lock";
        var request = new InstanceLockRequest { TtlSeconds = 0 };
        var content = JsonContent.Create(request);

        using var response = await _client.PatchAsync(lockToken, apiUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            throw await PlatformHttpResponseSnapshotException.CreateAndDisposeHttpResponse(response);
        }
    }
}

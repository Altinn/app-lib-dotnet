using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.AccessManagement;
using Altinn.App.Core.Internal.AccessManagement.Exceptions;
using Altinn.App.Core.Internal.AccessManagement.Helpers;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.App;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Infrastructure.Clients.AccessManagement;

/// <summary>
/// Client for interacting with the Access Management API.
/// This client is responsible for delegating and revoking rights for app instances.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="httpClient">The httpClient.</param>
/// <param name="appMetadata">The application metadata.</param>
/// <param name="accessTokenGenerator">The access token generator.</param>
/// <param name="platformSettings">The platform settings.</param>
/// <param name="telemetry">Telemetry.</param>
internal sealed class AccessManagementClient(
    ILogger<AccessManagementClient> logger,
    HttpClient httpClient,
    IAppMetadata appMetadata,
    IAccessTokenGenerator accessTokenGenerator,
    IOptions<PlatformSettings> platformSettings,
    Telemetry? telemetry = null
) : IAccessManagementClient
{
    private const string ApplicationJsonMediaType = "application/json";

    /// <inheritdoc />
    public async Task<DelegationResponse> DelegateRights(DelegationRequest delegation, CancellationToken ct = default)
    {
        using var activity = telemetry?.StartAppInstanceDelegationActivity();

        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;
        try
        {
            UrlHelper urlHelper = new(platformSettings.Value);
            var application = await appMetadata.GetApplicationMetadata();

            var uri = urlHelper.CreateInstanceDelegationUrl(delegation.ResourceId, delegation.InstanceId);
            var body = JsonSerializer.Serialize(DelegationRequest.ConvertToDto(delegation));
            logger.LogInformation(
                "Delegating rights to {DelegationTo} from {DelegationFrom} for {ResourceId}",
                delegation.To?.Value,
                delegation.From?.Value,
                delegation.ResourceId
            );

            using HttpRequestMessage httpRequestMessage = CreateRequestMessage(application, uri, body);
            using (httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, ct))
            {
                httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);
                return GetResponseOrThrow(httpResponseMessage, httpContent);
            }
        }
        catch (Exception e)
        {
            AccessManagementRequestException ex = CreateAccessManagementException(httpResponseMessage, httpContent, e);
            logger.LogError(e, "Error when processing access management delegate request.");
            throw ex;
        }
    }

    /// <inheritdoc />
    public async Task<DelegationResponse> RevokeRights(DelegationRequest delegation, CancellationToken ct = default)
    {
        using var activity = telemetry?.StartAppInstanceRevokeActivity();

        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;

        try
        {
            UrlHelper urlHelper = new(platformSettings.Value);
            var application = await appMetadata.GetApplicationMetadata();

            var uri = urlHelper.CreateInstanceRevokeUrl(delegation.ResourceId, delegation.InstanceId);
            var body = JsonSerializer.Serialize(DelegationRequest.ConvertToDto(delegation));
            logger.LogInformation(
                "Revoking rights from {DelegationTo} for {ResourceId}",
                delegation.To?.Value,
                delegation.ResourceId
            );

            using HttpRequestMessage httpRequestMessage = CreateRequestMessage(application, uri, body);
            using (httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, ct))
            {
                httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);
                return GetResponseOrThrow(httpResponseMessage, httpContent);
            }
        }
        catch (Exception e)
        {
            AccessManagementRequestException ex = CreateAccessManagementException(httpResponseMessage, httpContent, e);
            logger.LogError(e, "Error when processing access management revoke request.");
            throw ex;
        }
    }

    private DelegationResponse GetResponseOrThrow(HttpResponseMessage httpResponseMessage, string httpContent)
    {
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            logger.LogInformation($"===== ACCESS MANAGEMENT API ERROR =====");
            logger.LogInformation($"Status Code: {httpResponseMessage.StatusCode}");
            logger.LogInformation($"Response Headers: {httpResponseMessage.Headers}");
            logger.LogInformation($"Response Body: {httpContent}");
            try
            {
                var problemDetails = JsonSerializer.Deserialize<JsonElement>(httpContent);
                logger.LogInformation($"===== PARSED PROBLEM DETAILS =====");

                if (problemDetails.TryGetProperty("title", out var title))
                    logger.LogInformation($"Title: {title.GetString()}");
                if (problemDetails.TryGetProperty("detail", out var detail))
                    logger.LogInformation($"Detail: {detail.GetString()}");
                if (problemDetails.TryGetProperty("type", out var type))
                    logger.LogInformation($"Type: {type.GetString()}");
                if (problemDetails.TryGetProperty("status", out var status))
                    logger.LogInformation($"Status: {status.GetInt32()}");
                if (problemDetails.TryGetProperty("instance", out var instance))
                    logger.LogInformation($"Instance: {instance.GetString()}");
                if (problemDetails.TryGetProperty("errors", out var errors))
                    logger.LogInformation($"Errors: {errors.GetRawText()}");
                if (problemDetails.TryGetProperty("traceId", out var traceId))
                    logger.LogInformation($"TraceId: {traceId.GetString()}");

                logger.LogInformation($"=================================");
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Failed to parse ProblemDetails: {ex.Message}");
            }
            logger.LogInformation($"========================================");
            throw new HttpRequestException("Got error status code for access management request.");
        }
        DelegationResponse? response = JsonSerializer.Deserialize<DelegationResponse>(httpContent);
        return response ?? throw new JsonException("Couldn't deserialize access management response.");
    }

    private HttpRequestMessage CreateRequestMessage(Models.ApplicationMetadata application, string uri, string body)
    {
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body, new MediaTypeHeaderValue(ApplicationJsonMediaType)),
        };
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ApplicationJsonMediaType));
        var token = accessTokenGenerator.GenerateAccessToken(application.Org, application.AppIdentifier.App);
        httpRequestMessage.Headers.Add("PlatformAccessToken", token);

        Console.WriteLine($"===== OUTGOING DELEGATION REQUEST =====");
        Console.WriteLine($"App: {application.Org}/{application.AppIdentifier.App}");
        Console.WriteLine($"URL: {uri}");
        Console.WriteLine($"Method: {httpRequestMessage.Method}");
        Console.WriteLine(
            $"Headers: {string.Join(", ", httpRequestMessage.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}"
        );
        Console.WriteLine($"Body: {body}");
        Console.WriteLine($"Token (first 50 chars): {token?.Substring(0, Math.Min(50, token?.Length ?? 0))}...");
        Console.WriteLine($"=====================================");

        return httpRequestMessage;
    }

    private static AccessManagementRequestException CreateAccessManagementException(
        HttpResponseMessage? httpResponseMessage,
        string? httpContent,
        Exception e
    )
    {
        return e is AccessManagementRequestException exception
            ? exception
            : new AccessManagementRequestException(
                $"Something went wrong when processing the access management request.",
                null,
                httpResponseMessage?.StatusCode,
                httpContent,
                e
            );
    }
}

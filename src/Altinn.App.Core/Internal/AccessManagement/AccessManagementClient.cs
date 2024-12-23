using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.AccessManagement.Exceptions;
using Altinn.App.Core.Internal.AccessManagement.Helpers;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;
using Altinn.App.Core.Internal.App;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.AccessManagement;

internal interface IAccessManagementClient
{
    public Task<DelegationResponse> DelegateRights(DelegationRequest delegation, CancellationToken ct);
}

internal sealed class AccessManagementClient(
    ILogger<AccessManagementClient> logger,
    HttpClient httpClient,
    IAppMetadata appMetadata,
    IAccessTokenGenerator accessTokenGenerator,
    IOptions<PlatformSettings> platformSettings,
    Telemetry? telemetry = null
) : IAccessManagementClient
{
#pragma warning disable CA1822
    internal void DelegationCheck() { }
#pragma warning restore CA1822

    public async Task<DelegationResponse> DelegateRights(DelegationRequest delegation, CancellationToken ct)
    {
        // TODO: telemetry
        var onlyToRemoveWarning = telemetry?.IsInitialized;

        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;
        UrlHelper urlHelper = new(platformSettings.Value);
        try
        {
            var application = await appMetadata.GetApplicationMetadata();

            var uri = urlHelper.CreateInstanceDelegationUrl(delegation.ResourceId, delegation.InstanceId);
            AppsInstanceDelegationRequestDto dto = GetDto(delegation);
            var body = JsonSerializer.Serialize(dto);
            logger.LogInformation($"------------------------------------------------------------------------");
            logger.LogInformation($"Delegating rights to {uri} with body {body}");

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(body, new MediaTypeHeaderValue("application/json")),
            };
            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequestMessage.Headers.Add(
                "PlatformAccessToken",
                accessTokenGenerator.GenerateAccessToken(application.Org, application.AppIdentifier.App)
            );

            httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, ct);
            httpContent = await httpResponseMessage.Content.ReadAsStringAsync(ct);
            logger.LogInformation($"Response from delegation: {httpContent}");
            DelegationResponse? response;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                response = JsonSerializer.Deserialize<DelegationResponse>(httpContent);
                if (response is null)
                    throw new JsonException("Couldn't deserialize access management response.");
            }
            else
            {
                try
                {
                    var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(httpContent);
                    if (problemDetails is not null)
                    {
                        logger.LogError(
                            "Got error status code for access management request. Status code: {StatusCode}. Problem details: {ProblemDetails}",
                            httpResponseMessage.StatusCode,
                            JsonSerializer.Serialize(problemDetails)
                        );
                        throw new AccessManagementRequestException(
                            "Got error status code for access management request.",
                            problemDetails,
                            httpResponseMessage.StatusCode,
                            httpContent
                        );
                    }
                }
                catch (JsonException)
                {
                    response = null;
                }
                throw new HttpRequestException("Got error status code for access management request.");
            }
            return response;
        }
        catch (Exception e)
        {
            var ex =
                e is AccessManagementRequestException
                    ? e
                    : new AccessManagementRequestException(
                        $"Something went wrong when processing the access management request.",
                        null,
                        httpResponseMessage?.StatusCode,
                        httpContent,
                        e
                    );
            logger.LogError(ex, "Error when processing access management request.");

            // TODO: metrics

            throw ex;
        }
        finally
        {
            httpResponseMessage?.Dispose();
        }
    }

    private static AppsInstanceDelegationRequestDto GetDto(DelegationRequest delegation)
    {
        return new AppsInstanceDelegationRequestDto
        {
            From = new Delegator
            {
                Type = delegation.From is not null
                    ? delegation.From.Type
                    : throw new AccessManagementArgumentException("From is required"),
                Value = delegation.From.Value,
            },
            To = new Delegatee
            {
                Type = delegation.To is not null
                    ? delegation.To.Type
                    : throw new AccessManagementArgumentException("To is required"),
                Value = delegation.To.Value,
            },
            Rights = delegation
                .Rights.Select(r => new RightDto
                {
                    Resource = r.Resource.Select(rr => new Resource { Type = rr.Type, Value = rr.Value }).ToList(),
                    Action = new AltinnAction
                    {
                        Type = r.Action is not null
                            ? r.Action.Type
                            : throw new AccessManagementArgumentException("Action is required"),
                        Value = r.Action.Value,
                    },
                })
                .ToList(),
        };
    }
}

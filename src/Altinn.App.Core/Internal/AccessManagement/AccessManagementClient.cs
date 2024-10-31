using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.AccessManagement.Exceptions;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;
using Altinn.App.Core.Internal.App;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.AccessManagement;

internal interface IAccessManagementClient
{
    public Task DelegateSignRights(string taskId, Instance instance, string FromPartyId, string ToPartyId, CancellationToken ct);
}

internal sealed class AccessManagementClient(
    ILogger<AccessManagementClient> logger,
    HttpClient httpClient,
    IAppMetadata appMetadata,
    IAccessTokenGenerator accessTokenGenerator,
    Telemetry? telemetry = null
) : IAccessManagementClient
{
    internal void DelegationCheck() { }

    public async Task DelegateSignRights(string taskId, Instance instance, string FromPartyId, string ToPartyId, CancellationToken ct)
    {
        // TODO: telemetry
        // csharpier-ignore-start
        DelegationRequest delegation = DelegationRequestBuilder
            .CreateBuilder()
            .WithAppResourceId(instance.AppId) // TODO: translate app id to altinn resource id
            .WithInstanceId(instance.Id)
            .WithDelegator(new From { Type = DelegationConst.Party, Value = FromPartyId })
            .WithRecipient(new To { Type = DelegationConst.Party, Value = ToPartyId })
            .AddRight()
                .WithAction(DelegationConst.ActionId, ActionType.Read)
                .AddResource(DelegationConst.Resource, instance.AppId) // TODO: translate app id to altinn resource id
                .AddResource(DelegationConst.Task, taskId)
                .BuildRight()
            .AddRight()
                .WithAction(DelegationConst.ActionId, ActionType.Sign)
                .AddResource(DelegationConst.Resource, instance.AppId) // TODO: translate app id to altinn resource id
                .AddResource(DelegationConst.Task, taskId)
                .BuildRight()
            .Build();
        await DelegateRights(delegation, instance, ct);
        // csharpier-ignore-end
    }

    internal async Task<DelegationResponse> DelegateRights(DelegationRequest delegation, Instance instance, CancellationToken ct)
    {
        // TODO: telemetry
        HttpResponseMessage? httpResponseMessage = null;
        string? httpContent = null;
        try
        {
            var application = await appMetadata.GetApplicationMetadata();

            var uri = ""; // TODO
            var body = JsonSerializer.Serialize(delegation);

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
            DelegationResponse? response;
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                response = JsonSerializer.Deserialize<DelegationResponse>(httpContent);
                if (response is null)
                    throw new JsonException("Couldn't deserialize access management response.");
            }
            else
            {
                throw new HttpRequestException("Got error status code for access management request.");
            }
            return response;
        }
        catch (Exception e)
        {
            var ex = new DelegationException(
                $"Something went wrong when processing the access management request.",
                httpResponseMessage,
                httpContent,
                e
            );
            logger.LogError(ex, "Error when processing access management request.");

            throw ex;
        }
        finally
        {
            httpResponseMessage?.Dispose();
        }
    }
}

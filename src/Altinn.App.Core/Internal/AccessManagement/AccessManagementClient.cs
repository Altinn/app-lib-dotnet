using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.AccessManagement;

internal interface IAccessManagementClient
{
    public Task DelegateSignRights(string taskId, Instance instance, string FromPartyId, string ToPartyId);
}

internal sealed class AccessManagementClient(
// ILogger<AccessManagementClient> logger,
// HttpClient httpClient,
// IAppMetadata appMetadata,
// IAccessTokenGenerator accessTokenGenerator,
// Telemetry? telemetry = null
) : IAccessManagementClient
{
    internal void DelegationCheck() { }

    public async Task DelegateSignRights(string taskId, Instance instance, string FromPartyId, string ToPartyId)
    {
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
        await DelegateRights(delegation, instance);
        // csharpier-ignore-end
    }

    internal async Task DelegateRights(DelegationRequest delegation, Instance instance) { }
}

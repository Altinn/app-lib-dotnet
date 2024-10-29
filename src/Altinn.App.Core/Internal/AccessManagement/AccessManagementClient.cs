using System;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.App;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.AccessManagement;

internal interface IAccessManagementClient
{
    public Task DelegateSignRights(string taskId, Instance instance);
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

    // csharpier-ignore-start
    public async Task DelegateSignRights(string taskId, Instance instance)
    {
        DelegationRequest delegation = DelegationRequestBuilder
            .CreateBuilder()
            .WithAppResourceId(instance.AppId) // TODO: translate app id to altinn resource id
            .WithInstanceId(instance.Id)
            .WithDelegator(new From { Type = "urn:altinn:party:uuid", Value = "ff6fbedd-95ef-4de2-aed3-e6aeb292bd50" })
            .WithRecipient(new To { Type = "urn:altinn:party:uuid", Value = "c632a24e-910a-4332-a087-076bc98d600f" })
            .AddRight()
                .WithAction("urn:oasis:names:tc:xacml:1.0:action:action-id", "read")
                .AddResource("urn:altinn:resource", instance.AppId) // TODO: translate app id to altinn resource id
                .AddResource("urn:altinn:task", taskId)
                .BuildRight()
            .AddRight()
                .WithAction("urn:oasis:names:tc:xacml:1.0:action:action-id", "sign")
                .AddResource("urn:altinn:resource", instance.AppId) // TODO: translate app id to altinn resource id
                .AddResource("urn:altinn:task", taskId)
                .BuildRight()
            .Create();
        await DelegateRights(delegation, instance);
    }
    // csharpier-ignore-end
    internal async Task DelegateRights(DelegationRequest delegation, Instance instance) { }
}

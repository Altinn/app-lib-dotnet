using System.Text.Json.Serialization;
using Altinn.App.Core.Features.Notifications.Cancellation;
using Altinn.App.Core.Internal.Instances;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Endpoint(s) for the Altinn Notification microservice callback
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("{org}/{app}/notifications")]
public class NotificationCallbackController(
    ILogger<NotificationCallbackController> logger,
    ICancelInstantiationNotification instantiationNotification,
    IInstanceClient instanceClient
)
{
    /// <summary>
    /// Callback endpoint to check whether remaining notifications on application instantiation should be sent or not
    /// </summary>
    /// <returns><see cref="NotificationCallbackResponse"/></returns>
    [HttpGet("{instanceOwnerPartyId:int}/{instanceGuid:guid}")]
    public async Task<ActionResult<NotificationCallbackResponse>> NotificationCallback(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid
    )
    {
        Instance instance = await instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        logger.LogInformation(
            $"Received callback for org:{org}, app:{app}, instanceOwnerPartyId:{instanceOwnerPartyId}, instanceGuid:{instanceGuid}."
        );

        bool shouldSend = instantiationNotification.ShouldSend(instance);

        NotificationCallbackResponse response = new() { SendNotification = shouldSend };
        return response;
    }
}

/// <summary>
/// Callback response indicating whether instantiation notifications should be sent or not
/// </summary>
public sealed class NotificationCallbackResponse
{
    /// <summary>
    /// True if the notification should be sent, false if it should be cancelled
    /// </summary>
    [JsonPropertyName("sendNotification")]
    public bool SendNotification { get; set; }
}

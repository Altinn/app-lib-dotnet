using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Endpoint(s) for the Altinn Notification microservice callback
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("{org}/{app}/notifications")]
public class NotificationCallbackController(ILogger<NotificationCallbackController> logger)
{
    /// <summary>
    /// Callback endpoint to check whether remaining notifications on application instatiation should be cancelled
    /// </summary>
    /// <returns><see cref="NotificationCallbackResponse"/></returns>
    [HttpGet("{instanceOwnerPartyId:int}/{instanceGuid:guid}")]
    public ActionResult<NotificationCallbackResponse> ShouldCancel(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid
    )
    {
        logger.LogInformation(
            $"Received callback for org:{org}, app:{app}, instanceOwnerPartyId:{instanceOwnerPartyId}, instanceGuid:{instanceGuid}."
        );
        NotificationCallbackResponse response = new() { SendNotification = false };
        return response;
    }
}

/// <summary>
/// Callback response indication whether instantiation notifications should be cancelled or not
/// </summary>
public sealed class NotificationCallbackResponse
{
    /// <summary>
    /// True if the notification should be sent, false if it chould be cancelled
    /// </summary>
    [JsonPropertyName("sendNotification")]
    public bool SendNotification { get; set; }
}

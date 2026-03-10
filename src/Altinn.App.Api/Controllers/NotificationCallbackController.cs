using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Endpoint(s) for the Altinn Notification microservice callback
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("{org}/{app}/notifications")]
public class NotificationCallbackController
{
    /// <summary>
    /// Callback endpoint to check whether remaining notifications on application instatiation should be cancelled
    /// </summary>
    /// <returns>Boolean</returns>
    [HttpGet("instance")]
    public async Task<ActionResult<bool>> ShouldCancel()
    {
        return true;
    }
}

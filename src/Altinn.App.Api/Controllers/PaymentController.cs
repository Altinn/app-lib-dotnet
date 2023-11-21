using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Instances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// TODO: Describe controller
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/api/v1/payments")]
public class PaymentController(
    PaymentService _paymentService,
    IInstanceClient _instanceClient) : Controller
{
    /// <summary>
    /// Starts a new payment for the given instance
    /// </summary>
    [Authorize(Policy = AuthzConstants.POLICY_INSTANCE_WRITE)]
    [HttpGet("begin/{instanceOwnerId:int}/{instanceGuid:guid}")]
    public async Task<IActionResult> BeginPayment(string org, string app, int instanceOwnerId, Guid instanceGuid)
    {
        var instance = await _instanceClient.GetInstance(org, app, instanceOwnerId, instanceGuid);
        var paymentResult = await _paymentService.StartPayment(instance);
        return Ok(paymentResult);
        return Redirect(paymentResult.RedirectUrl);
    }
    
    /// <summary>
    /// Handles callbacks from the payment provider
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        await _paymentService.HandleCallback(Request);
        return Ok(); // Use default exception handling to indicate error
    }
}

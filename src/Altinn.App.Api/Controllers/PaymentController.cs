using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Core.Features.Payment.Services;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// TODO: Describe controller
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/api/v1/payments")]
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentController"/> class.
    /// </summary>
    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
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

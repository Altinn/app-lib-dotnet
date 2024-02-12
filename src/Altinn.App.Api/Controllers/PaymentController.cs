#nullable enable
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// TODO: Describe controller
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/api/v1/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/payment")]
public class PaymentController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IInstanceClient _instanceClient;
    private readonly IProcessReader _processReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentController"/> class.
    /// </summary>
    public PaymentController(IPaymentService paymentService, IInstanceClient instanceClient, IProcessReader processReader)
    {
        _paymentService = paymentService;
        _instanceClient = instanceClient;
        _processReader = processReader;
    }

    /// <summary>
    /// Handles webhook callback from the payment provider and updates payment status in storage.
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string app, string org, int instanceOwnerPartyId, Guid instanceGuid)
    {
        Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        AltinnPaymentConfiguration? paymentConfiguration = _processReader.GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)?.PaymentConfiguration;
        if (paymentConfiguration == null)
        {
            throw new PaymentException("Payment configuration not found in AltinnTaskExtension");
        }
        
        string? redirectUrl = await _paymentService.HandleCallback(instance, paymentConfiguration, Request);
        if (!string.IsNullOrEmpty(redirectUrl))
        {
            return Redirect(redirectUrl);
        }
        
        return Ok();
    }

    /// <summary>
    /// Handles the redirect back from payment provider. Does any necessary processing and redirects to the correct frontend page.
    /// </summary>
    [HttpGet("redirect")]
    public async Task<IActionResult> ReturnCallback(string app, string org, int instanceOwnerPartyId, Guid instanceGuid)
    {
        Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        
        string redirectUrl = await _paymentService.HandleRedirect(instance, Request);
        return Redirect(redirectUrl);
    }
}

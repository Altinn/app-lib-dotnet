using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller for handling payment operations.
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/payment")]
public class PaymentController : Controller
{
    private readonly IInstanceClient _instanceClient;
    private readonly IProcessReader _processReader;
    private readonly IPaymentService _paymentService;
    private readonly IOrderDetailsCalculator? _orderDetailsCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentController"/> class.
    /// </summary>
    public PaymentController(
        IInstanceClient instanceClient,
        IProcessReader processReader,
        IPaymentService paymentService,
        IOrderDetailsCalculator? orderDetailsCalculator = null
    )
    {
        _instanceClient = instanceClient;
        _processReader = processReader;
        _paymentService = paymentService;
        _orderDetailsCalculator = orderDetailsCalculator;
    }

    /// <summary>
    /// Get updated payment information for the instance. Will contact the payment processor to check the status of the payment. Current task must be a payment task. See payment related documentation.
    /// </summary>
    /// <param name="org">unique identifier of the organisation responsible for the app</param>
    /// <param name="app">application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerPartyId">unique id of the party that this the owner of the instance</param>
    /// <param name="instanceGuid">unique id to identify the instance</param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    /// <returns>An object containing updated payment information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaymentInformation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentInformation(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] string? language = null
    )
    {
        Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        AltinnPaymentConfiguration? paymentConfiguration = _processReader
            .GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)
            ?.PaymentConfiguration;

        if (paymentConfiguration == null)
        {
            throw new PaymentException("Payment configuration not found in AltinnTaskExtension");
        }

        PaymentInformation paymentInformation = await _paymentService.CheckAndStorePaymentStatus(
            instance,
            paymentConfiguration,
            language
        );
        return Ok(paymentInformation);
    }

    /// <summary>
    /// Run order details calculations and return the result. Does not require the current task to be a payment task.
    /// </summary>
    /// <param name="org">unique identifier of the organisation responsible for the app</param>
    /// <param name="app">application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerPartyId">unique id of the party that this the owner of the instance</param>
    /// <param name="instanceGuid">unique id to identify the instance</param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    /// <returns>An object containing updated payment information</returns>
    [HttpGet("order-details")]
    [ProducesResponseType(typeof(OrderDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderDetails(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] string? language = null
    )
    {
        if (_orderDetailsCalculator == null)
        {
            throw new PaymentException(
                "You must add an implementation of the IOrderDetailsCalculator interface to the DI container. See payment related documentation."
            );
        }

        Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        OrderDetails orderDetails = await _orderDetailsCalculator.CalculateOrderDetails(instance, language);

        return Ok(orderDetails);
    }
}

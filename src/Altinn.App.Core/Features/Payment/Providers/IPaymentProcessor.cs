using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Internal.Payment;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.App.Core.Features.Payment.Providers;

/// <summary>
/// Represents a payment processor that handles payment-related operations.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Starts a payment process for the specified instance and order details.
    /// </summary>
    /// <param name="instance">The instance for which the payment is being started.</param>
    /// <param name="orderDetails">The details of the order being paid.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the payment information.</returns>
    public Task<PaymentInformation> StartPayment(Instance instance, OrderDetails orderDetails);

    /// <summary>
    /// Cancels a payment for the specified instance and payment reference.
    /// </summary>
    /// <param name="instance">The instance for which the payment is being cancelled.</param>
    /// <param name="paymentReference">The reference of the payment to be cancelled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task CancelPayment(Instance instance, string paymentReference);

    /// <summary>
    /// Handles the callback from the payment provider.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="request">The HTTP request containing the callback data.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response from the payment provider.</returns>
    public Task<PaymentStatus?> HandleCallback(Instance instance, HttpRequest request);
}
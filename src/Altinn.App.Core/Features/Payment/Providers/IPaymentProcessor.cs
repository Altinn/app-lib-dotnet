using Altinn.App.Core.Features.Payment.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Payment.Providers;

/// <summary>
/// Represents a payment processor that handles payment-related operations.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Internal ID for the payment processor.
    /// </summary>
    public string PaymentProcessorId { get; }
    
    /// <summary>
    /// Starts a payment process for the specified instance and order details.
    /// </summary>
    /// <param name="instance">The instance for which the payment is being started.</param>
    /// <param name="orderDetails">The details of the order being paid.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the payment details.</returns>
    public Task<PaymentDetails> StartPayment(Instance instance, OrderDetails orderDetails);

    /// <summary>
    /// Cancels a payment for the specified instance and payment reference.
    /// </summary>
    /// <param name="instance">The instance for which the payment is being cancelled.</param>
    /// <param name="paymentInformation"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task<bool> CancelPayment(Instance instance, PaymentInformation paymentInformation);

    /// <summary>
    /// Gets the payment status for the specified instance and payment reference.
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="paymentReference"></param>
    /// <param name="expectedTotalIncVat"></param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response from the payment provider.</returns>
    public Task<PaymentStatus?> GetPaymentStatus(Instance instance, string paymentReference, decimal expectedTotalIncVat);
}
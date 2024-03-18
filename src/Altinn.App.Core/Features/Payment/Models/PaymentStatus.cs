namespace Altinn.App.Core.Features.Payment.Models
{
    /// <summary>
    /// Represents the status of a payment.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// The payment request has been created and sent to payment provider.
        /// </summary>
        Created,
        
        /// <summary>
        /// The payment has been paid.
        /// </summary>
        Paid,
        
        /// <summary>
        /// Something went wrong and the payment is considered failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The payment has been cancelled.
        /// </summary>
        Cancelled,
    }
}
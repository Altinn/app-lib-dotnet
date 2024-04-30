using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Payment.Models
{
    /// <summary>
    /// Represents payment information for a transaction.
    /// </summary>
    public class PaymentInformation
    {
        /// <summary>
        /// Gets or sets the taskId of the payment task this payment information is associated with.
        /// </summary>
        public required string TaskId { get; set; }

        /// <summary>
        /// Gets or sets the status of the payment.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required PaymentStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the order details for the transaction.
        /// </summary>
        public required OrderDetails OrderDetails { get; set; }

        /// <summary>
        /// Contains details about the payment, set by the payment processor implementation.
        /// </summary>
        public PaymentDetails? PaymentDetails { get; set; }
    }
}

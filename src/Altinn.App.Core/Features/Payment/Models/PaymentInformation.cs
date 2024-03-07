using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Payment.Models
{
    /// <summary>
    /// Represents payment information for a transaction.
    /// </summary>
    public class PaymentInformation
    {
        /// <summary>
        /// Gets or sets the redirect URL for the payment.
        /// </summary>
        public required string RedirectUrl { get; set; }

        /// <summary>
        /// Gets or sets the payment reference for the transaction.
        /// </summary>
        public required string PaymentReference { get; set; }

        /// <summary>
        /// Gets or sets the status of the payment.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the order details for the transaction.
        /// </summary>
        public OrderDetails? OrderDetails { get; set; }
    }
}

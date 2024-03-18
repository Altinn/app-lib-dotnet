using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Payment.Models;

/// <summary>
/// Represents the details of a payment
/// </summary>
public class PaymentDetails
{
    /// <summary>
    /// Gets or sets the payment reference for the transaction.
    /// </summary>
    public required string PaymentId { get; set; }

    /// <summary>
    /// Gets or sets the redirect URL for the payment.
    /// </summary>
    public required string RedirectUrl { get; set; }

    /// <summary>
    /// Contains a URL to the payment processor receipt, if available.
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// Gets or sets the status of the payment.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the order details for the transaction.
    /// </summary>
    public OrderDetails? OrderDetails { get; set; }
}
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
}
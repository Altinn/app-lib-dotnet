namespace Altinn.App.Core.Features.Payment.Models;

/// <summary>
/// Wrapping class to represent a payment order
/// </summary>
public class OrderDetails
{
    /// <summary>
    /// Optional reference to the order. Could be used by other systems to identify the order
    /// </summary>
    public string? OrderReference { get; set; }
    /// <summary>
    /// Monetary unit of the prices in the order.
    /// </summary>
    public required string Currency { get; set; }
    /// <summary>
    /// The lines that make up the order
    /// </summary>
    public required List<PaymentOrderLine> OrderLines { get; set; }
    /// <summary>
    /// Sum of all order line prices excluding VAT
    /// </summary>
    public decimal TotalPriceExVat => OrderLines.Sum(x => x.PriceExVat * x.Quantity);
    /// <summary>
    /// Sum of all order line VAT
    /// </summary>
    public decimal TotalVat => OrderLines.Sum(x => x.PriceExVat * x.Quantity * x.VatPercent / 100M);
    /// <summary>
    /// Total order price including VAT
    /// </summary>
    public decimal TotalPriceIncVat => OrderLines.Sum(l=> l.PriceExVat * l.Quantity * (1 + l.VatPercent / 100M));
}

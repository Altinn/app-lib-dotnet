namespace Altinn.App.Core.Features.Payment.Providers.Nets.Models;

/// <summary>
/// Specifies an order associated with a payment. An order must contain at least one order item. The amount of the order must match the sum of the specified order items.
/// </summary>
public class NetsOrder
{
    /// <summary>
    /// A list of order items. At least one item must be specified.
    /// </summary>
    public required List<NetsOrderItem> Items { get; set; }
    /// <summary>
    /// The total amount of the order including VAT, if any. (Sum of all grossTotalAmounts in the order.)
    /// Allowed: &gt;0
    /// </summary>
    public required int Amount { get; set; }
    /// <summary>
    /// The currency of the payment, for example 'SEK'.
    /// Length: 3
    /// The following special characters are not supported: &lt;,&gt;,\,’,”,&amp;,\\
    /// </summary>
    public required string Currency { get; set; }
    /// <summary>
    /// A reference to recognize this order. Usually a number sequence (order number).
    /// Length: 0-128
    /// The following special characters are not supported: &lt;,&gt;,\,’,”,&amp;,\\
    /// </summary>
    public string? Reference { get; set; }
    
}

public class NetsOrderItem
{
    /// <summary>
    /// A reference to recognize the product, usually the SKU (stock keeping unit) of the product. For convenience in the case of refunds or modifications of placed orders, the reference should be unique for each variation of a product item (size, color, etc).
    /// Length: 0-128
    /// The following special characters are not supported: &lt;,&gt;,\\
    /// </summary>
    public required string Reference { get; set; }
    
    /// <summary>
    /// The name of the product.
    /// Length: 0-128
    /// The following special characters are not supported: &lt;,&gt;,\\
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// The quantity of the product.
    /// Allowed: &gt;=0
    /// </summary>
    public required double Quantity { get; set; }
    
    /// <summary>
    /// The defined unit of measurement for the product, for example pcs, liters, or kg.
    /// Length: 0-128
    /// The following special characters are not supported: &lt;,&gt;,\,’,”,&amp;,\\
    /// </summary>
    public required string Unit { get; set; }
    
    /// <summary>
    /// The price per unit excluding VAT.
    /// Note: The amount can be negative.
    /// </summary>
    public required int UnitPrice { get; set; }

    /// <summary>
    /// The tax/VAT rate (in percentage times 100). For examlpe, the value 2500 corresponds to 25%. Defaults to 0 if not provided.
    /// </summary>
    public int? TaxRate { get; set; } = 0;

    /// <summary>
    /// The tax/VAT amount (unitPrice * quantity * taxRate / 10000). Defaults to 0 if not provided. taxAmount should include the total tax amount for the entire order item.
    /// </summary>
    public int? TaxAmount { get; set; } = 0;
    
    /// <summary>
    /// The total amount including VAT (netTotalAmount + taxAmount).
    /// Note: The amount can be negative.
    /// </summary>
    public required int GrossTotalAmount { get; set; }
    
    /// <summary>
    /// The total amount excluding VAT (unitPrice * quantity).
    /// Note: The amount can be negative.
    /// </summary>
    public required int NetTotalAmount { get; set; }
}
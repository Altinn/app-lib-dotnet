namespace Altinn.App.Core.Features.Payment.Providers;

public class PaymentStartResult
{
    public required string RedirectUrl { get; set; }
    public required string PaymentReference { get; set; }
}

namespace Altinn.App.Core.Features.Payment.Providers.Nets;

public class NetsPaymentSettings
{
    public required string SecretApiKey { get; set; }
    public required string BaseUrl { get; set; }
    public string IntegrationType { get; set; } = "HostedPaymentPage";
    public required string TermsUrl { get; set; }
    public bool ShowOrderSummary { get; set; } = true;
    public bool ShowMerchantName { get; set; } = true;
}
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;
using OrderDetails = Altinn.App.Core.Features.Payment.Models.OrderDetails;
using Altinn.App.Core.Features.Payment.Providers.Nets.Models;

namespace Altinn.App.Core.Features.Payment.Providers.Nets;

/// <summary>
/// Implementation of IPaymentProcessor for Nets. https://developer.nexigroup.com/nexi-checkout/en-EU/api/
/// </summary>
public class NetsPaymentProcessor : IPaymentProcessor
{
    private readonly NetsPaymentSettings _settings;
    private readonly GeneralSettings _generalSettings;
    private readonly INetsClient _netsClient;

    /// <summary>
    /// Amounts are specified in the lowest monetary unit for the given currency, without punctuation marks. For example: 100,00 NOK is specified as 10000 and 9.99 USD is specified as 999.
    /// Entering the amount 100 corresponds to 1 unit of the currency entered, such as e.g. 1 NOK.
    /// </summary>
    private const int LowestMonetaryUnitMultiplier = 100;

    /// <summary>
    /// Implementation of IPaymentProcessor for Nets.
    /// </summary>
    public NetsPaymentProcessor(INetsClient netsClient, IOptions<NetsPaymentSettings> settings,
        IOptions<GeneralSettings> generalSettings)
    {
        _netsClient = netsClient;
        _settings = settings.Value;
        _generalSettings = generalSettings.Value;
    }

    /// <inheritdoc />
    public string PaymentProcessorId => "Nets Easy";

    /// <inheritdoc />
    public async Task<PaymentDetails> StartPayment(Instance instance, OrderDetails orderDetails)
    {
        var instanceIdentifier = new InstanceIdentifier(instance);
        string baseUrl = _generalSettings.FormattedExternalAppBaseUrl(new AppIdentifier(instance));
        var altinnAppUrl = $"{baseUrl}#/instance/{instanceIdentifier}";

        var payment = new NetsCreatePayment()
        {
            Order = new NetsOrder
            {
                Amount = (int)(orderDetails.TotalPriceIncVat * LowestMonetaryUnitMultiplier),
                Currency = orderDetails.Currency,
                Reference = orderDetails.OrderReference,
                Items = orderDetails.OrderLines.Select(l => new NetsOrderItem()
                {
                    Reference = l.Id,
                    Name = l.Name,
                    Quantity = l.Quantity,
                    Unit = l.Unit,

                    UnitPrice = (int)(l.PriceExVat * LowestMonetaryUnitMultiplier),
                    GrossTotalAmount = (int)(l.PriceExVat * LowestMonetaryUnitMultiplier * l.Quantity *
                                             (1 + l.VatPercent / 100)),
                    NetTotalAmount = (int)(l.PriceExVat * LowestMonetaryUnitMultiplier * l.Quantity),
                    TaxAmount = (int)(l.PriceExVat * LowestMonetaryUnitMultiplier * l.Quantity * (l.VatPercent / 100)),
                    TaxRate = (int)(l.VatPercent * LowestMonetaryUnitMultiplier),
                }).ToList(),
            },
            MyReference = instance.Id.Split('/')[1],
            Checkout = new NetsCheckout
            {
                IntegrationType = "HostedPaymentPage",
                TermsUrl = _settings.TermsUrl,
                ReturnUrl = altinnAppUrl,
                CancelUrl = altinnAppUrl,
                Appearance = new NetsApparence
                {
                    DisplayOptions = new NetsApparence.NetsDisplayOptions
                    {
                        ShowOrderSummary = _settings.ShowOrderSummary,
                        ShowMerchantName = _settings.ShowMerchantName,
                    },
                },
                Charge = true
            },
        };

        HttpApiResult<NetsCreatePaymentSuccess> httpApiResult = await _netsClient.CreatePayment(payment);
        if (!httpApiResult.IsSuccess || httpApiResult.Result?.HostedPaymentPageUrl is null)
        {
            throw new PaymentException("Failed to create payment\n" + httpApiResult.Status + " - " +
                                       httpApiResult.RawError);
        }

        string hostedPaymentPageUrl = httpApiResult.Result.HostedPaymentPageUrl;
        string paymentId = httpApiResult.Result.PaymentId;

        return new PaymentDetails
        {
            PaymentId = paymentId,
            Status = PaymentStatus.Created,
            RedirectUrl = hostedPaymentPageUrl,
        };
    }

    /// <inheritdoc />
    public async Task<bool> TerminatePayment(Instance instance, PaymentInformation paymentInformation)
    {
        bool result = await _netsClient.TerminatePayment(paymentInformation.PaymentDetails.PaymentId);
        return result;
    }

    /// <inheritdoc />
    public async Task<PaymentStatus?> GetPaymentStatus(Instance instance, string paymentReference, decimal expectedTotalIncVat)
    {
        HttpApiResult<NetsPaymentFull> httpApiResult = await _netsClient.RetrievePayment(paymentReference);
        if (!httpApiResult.IsSuccess || httpApiResult.Result?.Payment is null)
        {
            throw new PaymentException("Failed to retrieve payment\n" + httpApiResult.Status + " - " +
                                       httpApiResult.RawError);
        }

        decimal? chargedAmount = httpApiResult.Result?.Payment?.Summary?.ChargedAmount;

        return chargedAmount > 0 ? PaymentStatus.Paid : PaymentStatus.Created;
    }
}
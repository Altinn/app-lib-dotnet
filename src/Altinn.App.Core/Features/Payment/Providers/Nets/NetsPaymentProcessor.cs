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
    private readonly IOrderDetailsCalculator? _orderDetailsFormatter;

    /// <summary>
    /// Implementation of IPaymentProcessor for Nets.
    /// </summary>
    /// <param name="netsClient"></param>
    /// <param name="settings"></param>
    /// <param name="generalSettings"></param>
    /// <param name="orderDetailsFormatter"></param>
    public NetsPaymentProcessor(INetsClient netsClient, IOptions<NetsPaymentSettings> settings,
        IOptions<GeneralSettings> generalSettings, IOrderDetailsCalculator? orderDetailsFormatter = null)
    {
        _netsClient = netsClient;
        _settings = settings.Value;
        _generalSettings = generalSettings.Value;
        _orderDetailsFormatter = orderDetailsFormatter;
    }

    /// <inheritdoc />
    public async Task<PaymentInformation> StartPayment(Instance instance, OrderDetails orderDetails)
    {
        if (_orderDetailsFormatter == null)
        {
            throw new PaymentException(
                "No IOrderDetailsFormatter implementation found. Implement the interface and add it as a transient service in Program.cs");
        }

        var instanceIdentifier = new InstanceIdentifier(instance);
        string baseUrl = _generalSettings.FormattedExternalAppBaseUrl(new AppIdentifier(instance));
        var altinnAppUrl = $"{baseUrl}#/instance/{instanceIdentifier}";
        
        /*
         * Amounts are specified in the lowest monetary unit for the given currency, without punctuation marks. For example: 100,00 NOK is specified as 10000 and 9.99 USD is specified as 999.
         * Entering the amount 100 corresponds to 1 unit of the currency entered, such as e.g. 1 NOK.
         */
        var payment = new NetsCreatePayment()
        {
            Order = new NetsOrder
            {
                Amount = (int)(orderDetails.TotalPriceIncVat * 100),
                Currency = orderDetails.Currency,
                Reference = orderDetails.OrderReference,
                Items = orderDetails.OrderLines.Select(l => new NetsOrderItem()
                {
                    Reference = l.Id,
                    Name = l.Name,
                    Quantity = l.Quantity,
                    Unit = l.Unit,

                    UnitPrice = (int)(l.PriceExVat * 100),
                    GrossTotalAmount = (int)(l.PriceExVat * 100 * l.Quantity * (1 + l.VatPercent / 100)),
                    NetTotalAmount = (int)(l.PriceExVat * 100 * l.Quantity),
                    TaxAmount = (int)(l.PriceExVat * 100 * l.Quantity * (l.VatPercent / 100)),
                    TaxRate = (int)(l.VatPercent * 100),
                }).ToList(),
            },
            MyReference = instance.Id.Split('/')[1],
            Checkout = new NetsCheckout
            {
                IntegrationType = _settings.IntegrationType,
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
            throw new PaymentException("Failed to create payment\n" + httpApiResult.Status + " - " + httpApiResult.RawError);
        }

        string hostedPaymentPageUrl = httpApiResult.Result.HostedPaymentPageUrl;
        string paymentId = httpApiResult.Result.PaymentId;

        return new PaymentInformation
        {
            PaymentReference = paymentId,
            RedirectUrl = hostedPaymentPageUrl,
            OrderDetails = orderDetails,
            Status = PaymentStatus.Created
        };
    }

    /// <inheritdoc />
    public async Task CancelPayment(Instance instance, string paymentReference)
    {
        await _netsClient.CancelPayment(paymentReference);
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

        if (chargedAmount is null or 0)
            return PaymentStatus.Created;
        
        // Amounts are specified in the lowest monetary unit for the given currency, without punctuation marks.
        var expectedTotalIncVatTimes100 =  (int)(100 * expectedTotalIncVat);
        return chargedAmount == expectedTotalIncVatTimes100 ? PaymentStatus.Paid : PaymentStatus.Failed;
    }
}
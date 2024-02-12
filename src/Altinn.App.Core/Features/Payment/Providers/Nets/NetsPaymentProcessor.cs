using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers.Nets.Models;
using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Payment.Providers.Nets;

/// <summary>
/// Implementation of IPaymentProcessor for Nets.
/// </summary>
public class NetsPaymentProcessor : IPaymentProcessor
{
    private readonly IOptions<NetsPaymentSettings> _settings;
    private readonly INetsClient _netsClient;
    private readonly IOrderDetailsFormatter? _orderDetailsFormatter;

    /// <summary>
    /// Implementation of IPaymentProcessor for Nets.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="netsClient"></param>
    /// <param name="orderDetailsFormatter"></param>
    public NetsPaymentProcessor(IOptions<NetsPaymentSettings> settings, INetsClient netsClient, IOrderDetailsFormatter? orderDetailsFormatter = null)
    {
        _settings = settings;
        _netsClient = netsClient;
        _orderDetailsFormatter = orderDetailsFormatter;
    }

    /// <inheritdoc />
    public async Task<PaymentInformation> StartPayment(Instance instance, OrderDetails orderDetails)
    {
        if (_orderDetailsFormatter == null)
        {
            throw new InvalidOperationException("No IOrderDetailsFormatter implementation found. Implement the interface and add it as a transient service in Program.cs");
        }

        var instanceIdentifier = new InstanceIdentifier(instance);
        var org = instance.Org;
        var app = instance.AppId.Split('/')[1];
        var payment = new NetsCreatePayment()
        {
            Order = new()
            {
                Amount = (int)(orderDetails.TotalPriceIncVat * 100),
                Currency = orderDetails.Currency,
                Reference = orderDetails.OrderReference,
                Items = orderDetails.OrderLines.Select(l => new NetsOrderItem()
                {
                    Reference = l.Id ?? string.Empty,
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
                IntegrationType = _settings.Value.IntegrationType,
                TermsUrl = _settings.Value.TermsUrl,
                ReturnUrl = $"https://{org}.apps.altinn.no/{org}/{app}/api/v1/instances/{instanceIdentifier}/payment/redirect",
                CancelUrl = $"https://{org}.apps.altinn.no/{org}/{app}/api/v1/instances/{instanceIdentifier}/payment/redirect",
                Appearance = new()
                {
                    DisplayOptions = new()
                    {
                        ShowOrderSummary = _settings.Value.ShowOrderSummary,
                        ShowMerchantName = _settings.Value.ShowMerchantName,
                    },
                },

            },
            Notifications = new NetsNotifications
            {
                WebHooks =
                [
                    new NetsWebHook
                    {
                        EventName = "payment.checkout.completed",
                        Authorization = "myAuthddd",
                        Url = $"https://{org}.apps.altinn.no/{org}/{app}/api/v1/instances/{instanceIdentifier}/payment/callback",
                    },
                ]
            }
        };
        var paymentCreateResult = await _netsClient.CreatePayment(payment);
        if (!paymentCreateResult.IsSuccess || paymentCreateResult.Success.HostedPaymentPageUrl is null)
        {
            throw new InvalidOperationException("Failed to create payment\n" + paymentCreateResult.RawError);
        }

        var url = paymentCreateResult.Success.HostedPaymentPageUrl;
        var paymentId = paymentCreateResult.Success.PaymentId;

        return new PaymentInformation
        {
            PaymentReference = paymentId,
            RedirectUrl = url,
            OrderDetails = orderDetails
        };
    }

    /// <inheritdoc />
    public async Task CancelPayment(Instance instance, string paymentReference)
    {
        await _netsClient.CancelPayment(paymentReference);
    }

    /// <inheritdoc />
    public async Task<PaymentStatus?> HandleCallback(Instance instance, HttpRequest request)
    {
        var body = await request.ReadFromJsonAsync<NetsWebhookEvent>();
        if (body == null)
        {
            throw new PaymentException("Unable to read NetsWebhookEvent from the request body");
        }
        
        string eventName = body.Event;
        var failedEvents = new[] { "payment.reservation.failed", "payment.cancel.failed"};
        var cancelledEvents = new[] { "payment.charge.failed", };
         
        if(eventName == "payment.checkout.completed")
        {
            return (PaymentStatus.Paid);
        }

        if (cancelledEvents.Contains(eventName))
        {
            return PaymentStatus.Cancelled;
        }

        if (failedEvents.Contains(eventName))
        {
            return (PaymentStatus.Failed);
        }

        return null;
    }
}
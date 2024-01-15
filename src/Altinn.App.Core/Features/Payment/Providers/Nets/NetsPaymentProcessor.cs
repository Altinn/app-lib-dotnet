using Altinn.App.Core.Features.Payment.Providers.Nets.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Payment.Providers.Nets;

/// <summary>
/// 
/// </summary>
public class NetsPaymentProcessor : IPaymentProcessor
{
    private readonly IOptions<NetsPaymentSettings> _settings;
    private readonly INetsClient _netsClient;
    private readonly IOrderDetailsFormatter? _orderDetailsFormatter;

    /// <summary>
    /// 
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
    public async Task<PaymentStartResult> StartPayment(Instance instance)
    {
        if (_orderDetailsFormatter == null)
        {
            throw new Exception("No IOrderDetailsFormatter implementation found. Implement the interface and add it as a transient service in Program.cs");
        }

        var orderDetails = await _orderDetailsFormatter.GetOrderDetails(instance);
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
                ReturnUrl = $"https://{org}.apps.altinn.no/{org}/{app}/api/v1/paymentCallback",
                CancelUrl = $"https://{org}.apps.altinn.no/{org}/{app}/api/v1/paymentCallback",
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
                        Url = $"https://{org}.apps.altinn.no/{org}/{app}/api/v1/paymentCallback",
                    }
                ]
            }
        };
        var paymentCreateResult = await _netsClient.CreatePayment(payment);
        if (!paymentCreateResult.IsSuccess || paymentCreateResult.Success.HostedPaymentPageUrl is null)
        {
            throw new Exception("Failed to create payment\n" + paymentCreateResult.RawError);
        }

        var url = paymentCreateResult.Success.HostedPaymentPageUrl; // TODO: Return url also
        var paymentId = paymentCreateResult.Success.PaymentId;

        return new PaymentStartResult
        {
            RedirectUrl = url,
            PaymentReference = paymentId,
        };
    }

    /// <inheritdoc />
    public async Task<string> HandleCallback(HttpRequest request)
    {
        throw new NotImplementedException();
    }
}
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Internal.Payment;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.App.Core.Features.Payment.Services;

/// <summary>
/// Service that wraps most payment related features
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IOrderDetailsFormatter _orderDetailsFormatter;

    /// <inheritdoc/>
    public PaymentService(IPaymentProcessor paymentProcessor, IOrderDetailsFormatter orderDetailsFormatter)
    {
        _paymentProcessor = paymentProcessor;
        _orderDetailsFormatter = orderDetailsFormatter;
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation> StartPayment(Instance instance)
    {
        var orderDetails = await _orderDetailsFormatter.GetOrderDetails(instance);

        return await _paymentProcessor.StartPayment(instance, orderDetails);
    }

    /// <inheritdoc/>
    public async Task CancelPayment(Instance instance, PaymentInformation paymentInformation)
    {
        await _paymentProcessor.CancelPayment(instance, paymentInformation.PaymentReference);
    }

    /// <inheritdoc/>
    public async Task HandleCallback(HttpRequest request)
    {
        await _paymentProcessor.HandleCallback(request);
    }
}
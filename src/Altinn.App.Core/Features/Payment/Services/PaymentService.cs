using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Payment.Services;

/// <summary>
/// Service that wraps most payment related features
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IOrderDetailsCalculator _orderDetailsCalculator;
    private readonly IDataService _dataService;
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    /// <param name="paymentProcessor"></param>
    /// <param name="orderDetailsCalculator"></param>
    /// <param name="dataService"></param>
    /// <param name="logger"></param>
    public PaymentService(IPaymentProcessor paymentProcessor, IOrderDetailsCalculator orderDetailsCalculator, IDataService dataService,
        ILogger<PaymentService> logger)
    {
        _paymentProcessor = paymentProcessor;
        _orderDetailsCalculator = orderDetailsCalculator;
        _dataService = dataService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation> StartPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");

        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if (paymentInformation != null)
        {
            if (paymentInformation.Status != PaymentStatus.Paid)
            {
                _logger.LogWarning(
                    "Payment with payment reference {paymentReference} already started for instance {instanceId}. Trying to cancel before creating new payment.",
                    paymentInformation.PaymentReference, instance.Id);
                await CancelAndDelete(instance, dataElementId, paymentInformation);
            }
            else
            {
                throw new PaymentException(
                    $"Payment with payment reference {paymentInformation.PaymentReference} already paid for instance {instance.Id}. Cannot start new payment.");
            }
        }

        OrderDetails orderDetails = await _orderDetailsCalculator.CalculateOrderDetails(instance);
        PaymentInformation startedPayment = await _paymentProcessor.StartPayment(instance, orderDetails);

        await _dataService.InsertJsonObject(new InstanceIdentifier(instance), dataTypeId, startedPayment);
        return startedPayment;
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation?> CheckAndStorePaymentInformation(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");
        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if (paymentInformation == null)
        {
            return null;
        }

        decimal totalPriceIncVat = paymentInformation.OrderDetails?.TotalPriceIncVat ?? 0;
        PaymentStatus? paymentStatus = await _paymentProcessor.GetPaymentStatus(instance, paymentInformation.PaymentReference, totalPriceIncVat);

        if (paymentStatus == null)
        {
            throw new PaymentException($"Unable to check payment status for instance {instance.Id}.");
        }

        paymentInformation.Status = paymentStatus.Value;

        await _dataService.UpdateJsonObject(new InstanceIdentifier(instance), dataTypeId, dataElementId, paymentInformation);
        return paymentInformation;
    }

    /// <inheritdoc/>
    public async Task CancelPaymentIfNotPaid(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");
        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if (paymentInformation != null && paymentInformation.Status != PaymentStatus.Paid)
        {
            await CancelAndDelete(instance, dataElementId, paymentInformation);
        }
    }

    private async Task CancelAndDelete(Instance instance, Guid dataElementId, PaymentInformation paymentInformation)
    {
        bool success = await _paymentProcessor.CancelPayment(instance, paymentInformation);

        if (!success)
        {
            throw new PaymentException("Unable to cancel existing payment.");
        }

        _logger.LogDebug("Payment {paymentReference} cancelled for instance {instanceId}. Deleting payment information.",
            paymentInformation.PaymentReference, instance.Id);

        await _dataService.DeleteById(new InstanceIdentifier(instance), dataElementId);

        _logger.LogDebug("Payment information for payment {paymentReference} deleted for instance {instanceId}.", paymentInformation.PaymentReference,
            instance.Id);
    }
}
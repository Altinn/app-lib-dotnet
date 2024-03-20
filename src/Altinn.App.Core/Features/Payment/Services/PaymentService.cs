using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Internal.App;
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
    private readonly IEnumerable<IPaymentProcessor> _paymentProcessors;
    private readonly IOrderDetailsCalculator _orderDetailsCalculator;
    private readonly IDataService _dataService;
    private readonly ILogger<PaymentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    public PaymentService(IEnumerable<IPaymentProcessor> paymentProcessors, IOrderDetailsCalculator orderDetailsCalculator, IDataService dataService,
        ILogger<PaymentService> logger)
    {
        _paymentProcessors = paymentProcessors;
        _orderDetailsCalculator = orderDetailsCalculator;
        _dataService = dataService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation> StartPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        _logger.LogInformation("Starting payment for instance {instanceId}.", instance.Id);

        ValidateConfig(paymentConfiguration);
        IPaymentProcessor paymentProcessor = _paymentProcessors.FirstOrDefault(p => p.PaymentProcessorId == paymentConfiguration.PaymentProcessorId) ??
                                             throw new PaymentException($"Payment processor with ID '{paymentConfiguration.PaymentProcessorId}' not found.");

        string dataTypeId = paymentConfiguration.PaymentDataType!;

        (Guid dataElementId, PaymentInformation? existingPaymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);
        if (existingPaymentInformation != null)
        {
            if (existingPaymentInformation.PaymentDetails.Status != PaymentStatus.Paid)
            {
                _logger.LogWarning(
                    "Payment with payment reference {paymentReference} already started for instance {instanceId}. Trying to cancel before creating new payment.",
                    existingPaymentInformation.PaymentDetails.PaymentId, instance.Id);
                await CancelAndDelete(instance, dataElementId, existingPaymentInformation);
            }
            else
            {
                throw new PaymentException(
                    $"Payment with payment reference {existingPaymentInformation.PaymentDetails.PaymentId} already paid for instance {instance.Id}. Cannot start new payment.");
            }
        }

        OrderDetails orderDetails = await _orderDetailsCalculator.CalculateOrderDetails(instance);
        PaymentDetails startedPayment = await paymentProcessor.StartPayment(instance, orderDetails);

        PaymentInformation paymentInformation = new()
        {
            TaskId = instance.Process.CurrentTask.ElementId,
            PaymentProcessorId = paymentProcessor.PaymentProcessorId,
            OrderDetails = orderDetails,
            PaymentDetails = startedPayment
        };

        await _dataService.InsertJsonObject(new InstanceIdentifier(instance), dataTypeId, paymentInformation);
        return paymentInformation;
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation?> CheckAndStorePaymentStatus(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        _logger.LogInformation("Checking payment status for instance {instanceId}.", instance.Id);

        ValidateConfig(paymentConfiguration);
        string dataTypeId = paymentConfiguration.PaymentDataType!;
        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        // TODO: Consider if this should be put in storage in process task start.
        if (paymentInformation == null)
        {
            return new PaymentInformation
            {
                TaskId = instance.Process.CurrentTask.ElementId,
                PaymentProcessorId = paymentConfiguration.PaymentProcessorId!,
                OrderDetails = await _orderDetailsCalculator.CalculateOrderDetails(instance),
            };
        }

        IPaymentProcessor paymentProcessor = _paymentProcessors.FirstOrDefault(p => p.PaymentProcessorId == paymentConfiguration.PaymentProcessorId) ??
                                             throw new PaymentException($"Payment processor with ID '{paymentConfiguration.PaymentProcessorId}' not found.");

        PaymentDetails paymentDetails = paymentInformation.PaymentDetails!;
        decimal totalPriceIncVat = paymentInformation.OrderDetails?.TotalPriceIncVat ?? 0;

        PaymentStatus? paymentStatus = await paymentProcessor.GetPaymentStatus(instance, paymentDetails.PaymentId, totalPriceIncVat);
        if (paymentStatus == null)
        {
            throw new PaymentException($"Unable to check payment status for instance {instance.Id}.");
        }

        paymentDetails.Status = paymentStatus.Value;

        await _dataService.UpdateJsonObject(new InstanceIdentifier(instance), dataTypeId, dataElementId, paymentInformation);
        return paymentInformation;
    }

    private async Task CancelAndDelete(Instance instance, Guid dataElementId, PaymentInformation paymentInformation)
    {
        IPaymentProcessor paymentProcessor = _paymentProcessors.FirstOrDefault(pp => pp.PaymentProcessorId == paymentInformation.PaymentProcessorId) ??
                                             throw new PaymentException($"Payment processor with ID '{paymentInformation.PaymentProcessorId}' not found.");

        bool success = await paymentProcessor.TerminatePayment(instance, paymentInformation);

        if (!success)
        {
            throw new PaymentException(
                $"Unable to cancel existing {paymentInformation.PaymentProcessorId} payment with ID: {paymentInformation.PaymentDetails.PaymentId}.");
        }

        _logger.LogDebug("Payment {paymentReference} cancelled for instance {instanceId}. Deleting payment information.",
            paymentInformation.PaymentDetails.PaymentId, instance.Id);

        await _dataService.DeleteById(new InstanceIdentifier(instance), dataElementId);

        _logger.LogDebug("Payment information for payment {paymentReference} deleted for instance {instanceId}.", paymentInformation.PaymentDetails.PaymentId,
            instance.Id);
    }

    private static void ValidateConfig(AltinnPaymentConfiguration paymentConfiguration)
    {
        List<string> errorMessages = [];

        if (string.IsNullOrWhiteSpace(paymentConfiguration.PaymentProcessorId))
        {
            errorMessages.Add("PaymentProcessorId is missing.");
        }

        if (string.IsNullOrWhiteSpace(paymentConfiguration.PaymentDataType))
        {
            errorMessages.Add("PaymentDataType is missing.");
        }

        if (errorMessages.Count != 0)
        {
            throw new ApplicationConfigException("Payment process task configuration is not valid: " + string.Join(",\n", errorMessages));
        }
    }
}
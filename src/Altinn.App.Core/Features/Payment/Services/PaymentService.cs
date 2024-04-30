using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Processors;
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
    public PaymentService(
        IEnumerable<IPaymentProcessor> paymentProcessors,
        IOrderDetailsCalculator orderDetailsCalculator,
        IDataService dataService,
        ILogger<PaymentService> logger
    )
    {
        _paymentProcessors = paymentProcessors;
        _orderDetailsCalculator = orderDetailsCalculator;
        _dataService = dataService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<(PaymentInformation paymentInformation, bool alreadyPaid)> StartPayment(
        Instance instance,
        AltinnPaymentConfiguration paymentConfiguration,
        string? language
    )
    {
        _logger.LogInformation("Starting payment for instance {instanceId}.", instance.Id);

        ValidateConfig(paymentConfiguration);

        _logger.LogInformation("Payment config is valid for instance {instanceId}.", instance.Id);
        string dataTypeId = paymentConfiguration.PaymentDataType!;

        (Guid dataElementId, PaymentInformation? existingPaymentInformation) =
            await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);
        if (existingPaymentInformation?.PaymentDetails != null)
        {
            if (existingPaymentInformation.Status != PaymentStatus.Paid)
            {
                _logger.LogWarning(
                    "Payment with payment id {paymentId} already started for instance {instanceId}. Trying to cancel before creating new payment.",
                    existingPaymentInformation.PaymentDetails.PaymentId,
                    instance.Id
                );
                await CancelAndDelete(instance, dataElementId, existingPaymentInformation);
            }
            else
            {
                _logger.LogWarning(
                    "Payment with payment id {paymentId} already paid for instance {instanceId}. Cannot start new payment.",
                    existingPaymentInformation.PaymentDetails.PaymentId,
                    instance.Id
                );

                return (existingPaymentInformation, true);
            }
        }

        OrderDetails orderDetails = await _orderDetailsCalculator.CalculateOrderDetails(instance, language);
        IPaymentProcessor paymentProcessor =
            _paymentProcessors.FirstOrDefault(p => p.PaymentProcessorId == orderDetails.PaymentProcessorId)
            ?? throw new PaymentException(
                $"Payment processor with ID '{orderDetails.PaymentProcessorId}' not found for instance {instance.Id}."
            );

        _logger.LogInformation(
            "Payment processor {paymentProviderId} will be used for payment for instance {instanceId}.",
            paymentProcessor.PaymentProcessorId,
            instance.Id
        );

        //If the sum of the order is 0, we can skip invoking the payment processor.
        PaymentDetails? startedPayment =
            orderDetails.TotalPriceIncVat > 0
                ? await paymentProcessor.StartPayment(instance, orderDetails, language)
                : null;

        if (startedPayment != null)
        {
            _logger.LogInformation("Payment started successfully for instance {instanceId}.", instance.Id);
        }
        else
        {
            _logger.LogInformation(
                "Skipping starting payment since order sum is zero for instance {instanceId}.",
                instance.Id
            );
        }

        PaymentInformation paymentInformation =
            new()
            {
                TaskId = instance.Process.CurrentTask.ElementId,
                Status = startedPayment != null ? PaymentStatus.Created : PaymentStatus.Skipped,
                OrderDetails = orderDetails,
                PaymentDetails = startedPayment
            };

        await _dataService.InsertJsonObject(new InstanceIdentifier(instance), dataTypeId, paymentInformation);
        return (paymentInformation, false);
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation> CheckAndStorePaymentStatus(
        Instance instance,
        AltinnPaymentConfiguration paymentConfiguration,
        string? language
    )
    {
        _logger.LogInformation("Checking payment status for instance {instanceId}.", instance.Id);

        ValidateConfig(paymentConfiguration);
        string dataTypeId = paymentConfiguration.PaymentDataType!;
        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(
            instance,
            dataTypeId
        );

        if (paymentInformation == null)
        {
            _logger.LogInformation(
                "No payment information stored yet for instance {instanceId}. Returning uninitialized result.",
                instance.Id
            );

            return new PaymentInformation
            {
                TaskId = instance.Process.CurrentTask.ElementId,
                Status = PaymentStatus.Uninitialized,
                OrderDetails = await _orderDetailsCalculator.CalculateOrderDetails(instance, language),
            };
        }

        PaymentDetails paymentDetails = paymentInformation.PaymentDetails!;
        decimal totalPriceIncVat = paymentInformation.OrderDetails.TotalPriceIncVat;
        string paymentProcessorId = paymentInformation.OrderDetails.PaymentProcessorId;

        if (paymentInformation.Status == PaymentStatus.Skipped)
        {
            _logger.LogInformation(
                "Payment status is '{skipped}' for instance {instanceId}. Won't ask payment processor for status.",
                PaymentStatus.Skipped.ToString(),
                instance.Id
            );

            return paymentInformation;
        }

        IPaymentProcessor paymentProcessor =
            _paymentProcessors.FirstOrDefault(p => p.PaymentProcessorId == paymentProcessorId)
            ?? throw new PaymentException($"Payment processor with ID '{paymentProcessorId}' not found.");

        (PaymentStatus paymentStatus, PaymentDetails updatedPaymentDetails) = await paymentProcessor.GetPaymentStatus(
            instance,
            paymentDetails.PaymentId,
            totalPriceIncVat,
            language
        );

        paymentInformation.Status = paymentStatus;
        paymentInformation.PaymentDetails = updatedPaymentDetails;

        _logger.LogInformation(
            "Updated payment status is {status} for instance {instanceId}.",
            paymentInformation.Status.ToString(),
            instance.Id
        );

        await _dataService.UpdateJsonObject(
            new InstanceIdentifier(instance),
            dataTypeId,
            dataElementId,
            paymentInformation
        );

        return paymentInformation;
    }

    /// <inheritdoc/>
    public async Task<bool> IsPaymentCompleted(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        ValidateConfig(paymentConfiguration);

        string dataTypeId = paymentConfiguration.PaymentDataType!;
        (Guid _, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(
            instance,
            dataTypeId
        );

        if (paymentInformation == null)
        {
            throw new PaymentException("Payment information not found.");
        }

        return paymentInformation.Status is PaymentStatus.Paid or PaymentStatus.Skipped;
    }

    /// <inheritdoc/>
    public async Task CancelAndDelete(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        ValidateConfig(paymentConfiguration);

        string dataTypeId = paymentConfiguration.PaymentDataType!;
        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(
            instance,
            dataTypeId
        );

        if (paymentInformation == null)
            return;

        await CancelAndDelete(instance, dataElementId, paymentInformation);
    }

    private async Task CancelAndDelete(Instance instance, Guid dataElementId, PaymentInformation paymentInformation)
    {
        string paymentProcessorId = paymentInformation.OrderDetails.PaymentProcessorId;
        IPaymentProcessor paymentProcessor =
            _paymentProcessors.FirstOrDefault(pp => pp.PaymentProcessorId == paymentProcessorId)
            ?? throw new PaymentException($"Payment processor with ID '{paymentProcessorId}' not found.");

        bool success = await paymentProcessor.TerminatePayment(instance, paymentInformation);
        string paymentId = paymentInformation.PaymentDetails?.PaymentId ?? "missing";

        if (!success)
        {
            throw new PaymentException($"Unable to cancel existing {paymentProcessorId} payment with ID: {paymentId}.");
        }

        _logger.LogDebug(
            "Payment {paymentReference} cancelled for instance {instanceId}. Deleting payment information.",
            paymentId,
            instance.Id
        );

        await _dataService.DeleteById(new InstanceIdentifier(instance), dataElementId);

        _logger.LogDebug(
            "Payment information for payment {paymentReference} deleted for instance {instanceId}.",
            paymentId,
            instance.Id
        );
    }

    private static void ValidateConfig(AltinnPaymentConfiguration paymentConfiguration)
    {
        List<string> errorMessages = [];

        if (string.IsNullOrWhiteSpace(paymentConfiguration.PaymentDataType))
        {
            errorMessages.Add("PaymentDataType is missing.");
        }

        if (errorMessages.Count != 0)
        {
            throw new ApplicationConfigException(
                "Payment process task configuration is not valid: " + string.Join(",\n", errorMessages)
            );
        }
    }
}

using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Payment.Services;

/// <summary>
/// Service that wraps most payment related features
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IOrderDetailsCalculator _orderDetailsCalculator;
    private readonly IDataService _dataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    /// <param name="paymentProcessor"></param>
    /// <param name="orderDetailsCalculator"></param>
    /// <param name="dataService"></param>
    public PaymentService(IPaymentProcessor paymentProcessor, IOrderDetailsCalculator orderDetailsCalculator, IDataService dataService)
    {
        _paymentProcessor = paymentProcessor;
        _orderDetailsCalculator = orderDetailsCalculator;
        _dataService = dataService;
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation> StartPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");

        (Guid _, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if (paymentInformation != null && paymentInformation.Status != PaymentStatus.Paid)
        {
            //TODO: Logg at det allerede finnes en payment, og at vi prøver å starte en ny. Warning.
            await CancelPayment(instance, paymentConfiguration);
            //TODO: Hvis jeg ikke skriver om logikken rundt storage så må jeg slette her,
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

        if(paymentInformation == null)
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
    public async Task CancelPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");

        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if (paymentInformation != null && paymentInformation.Status != PaymentStatus.Paid)
        {
            await _paymentProcessor.CancelPayment(instance, paymentInformation.PaymentReference);
            //TODO: Hvis cancel feiler, ikke slett i hvert fall.
            await _dataService.DeleteById(new InstanceIdentifier(instance), dataElementId); //TODO: Fjerne, og heller ta vare på historikk frem til vi vet at det er cancelled i Nets.
        } 
    }
}
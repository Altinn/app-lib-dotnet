using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
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
    private readonly IDataService _dataService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    /// <param name="paymentProcessor"></param>
    /// <param name="orderDetailsFormatter"></param>
    /// <param name="dataService"></param>
    public PaymentService(IPaymentProcessor paymentProcessor, IOrderDetailsFormatter orderDetailsFormatter, IDataService dataService, IProcessReader processReader)
    {
        _paymentProcessor = paymentProcessor;
        _orderDetailsFormatter = orderDetailsFormatter;
        _dataService = dataService;
    }

    /// <inheritdoc/>
    public async Task<PaymentInformation> StartPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");

        (Guid _, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if (paymentInformation != null && paymentInformation.Status != PaymentStatus.Paid)
        {
            await CancelPayment(instance, paymentConfiguration);
        }
        
        OrderDetails orderDetails = await _orderDetailsFormatter.GetOrderDetails(instance);
        PaymentInformation startedPayment = await _paymentProcessor.StartPayment(instance, orderDetails);

        await _dataService.InsertJsonObject(new InstanceIdentifier(instance), dataTypeId, startedPayment);
        return startedPayment;
    }
 
    /// <inheritdoc/>
    public async Task CancelPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");

        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if (paymentInformation != null && paymentInformation.Status != PaymentStatus.Paid)
        {
            await _paymentProcessor.CancelPayment(instance, paymentInformation.PaymentReference);
            await _dataService.DeleteById(new InstanceIdentifier(instance), dataElementId);
        } 
    }

    /// <inheritdoc/>
    public async Task HandleCallback(Instance instance, AltinnPaymentConfiguration paymentConfiguration, HttpRequest request)
    {
        string dataTypeId = paymentConfiguration.PaymentDataType ?? throw new PaymentException("PaymentDataType not found in paymentConfiguration");
        PaymentStatus? paymentStatus = await _paymentProcessor.HandleCallback(instance, request);

        if (paymentStatus == null)
            return;
        
        (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

        if(paymentInformation == null)
        {
            throw new PaymentException("Payment information not found");
        }
        
        paymentInformation.Status = paymentStatus.Value;
        await _dataService.UpdateJsonObject(new InstanceIdentifier(instance), dataTypeId, dataElementId, paymentInformation);
    }

    /// <inheritdoc/>
    public Task<string> HandleRedirect(Instance instance, HttpRequest request)
    {
        string app = instance.AppId.Split('/')[1];
        var instanceIdentifier = new InstanceIdentifier(instance);
        
        //TODO: Handle local/test/production environment
        return Task.FromResult($"http://local.altinn.cloud:8080/{instance.Org}/{app}/#/instance/{instanceIdentifier}");
    }
}
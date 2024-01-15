using System.Text.Json;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.App.Core.Features.Payment.Services;

/// <summary>
/// Service that wraps most payment related features
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly IDataClient _dataClient;

    /// <inheritdoc/>
    public PaymentService(IPaymentProcessor paymentProcessor, IDataClient dataClient)
    {
        _paymentProcessor = paymentProcessor;
        _dataClient = dataClient;
    }

    /// <inheritdoc/>
    public async Task<PaymentStartResult> StartPayment(Instance instance)
    {
        var reference = await _paymentProcessor.StartPayment(instance);
        using var referenceStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(referenceStream, reference, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        referenceStream.Position = 0;
        await _dataClient.InsertBinaryData(instance.Id, "payment-reference", "application/text", "payment-reference", referenceStream);
        return reference;
    }

    /// <inheritdoc/>
    public async Task HandleCallback(HttpRequest request)
    {
        await _paymentProcessor.HandleCallback(request);
    }
}
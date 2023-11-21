using Altinn.App.Core.Features.Payment.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.App.Core.Features.Payment.Providers;

public interface IPaymentProcessor
{
    public Task<PaymentStartResult> StartPayment(Instance instance, PaymentOrder order);
    public Task<string> HandleCallback(HttpRequest request);
}
using Altinn.App.Core.Features.Payment.Providers;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.App.Core.Features.Payment.Services
{
    /// <summary>
    /// Service for handling payment.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Start payment for an instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        Task<PaymentStartResult> StartPayment(Instance instance);

        /// <summary>
        /// Handle callback from payment provider.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task HandleCallback(HttpRequest request);

    }
}
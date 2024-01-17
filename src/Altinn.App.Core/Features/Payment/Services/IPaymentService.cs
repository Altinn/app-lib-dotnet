using Altinn.App.Core.Internal.Payment;
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
        Task<PaymentInformation> StartPayment(Instance instance);

        /// <summary>
        /// Cancel payment for an instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="paymentInformation"></param>
        /// <returns></returns>
        Task CancelPayment(Instance instance, PaymentInformation paymentInformation);

        /// <summary>
        /// Handle callback from payment provider.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task HandleCallback(HttpRequest request);
    }
}
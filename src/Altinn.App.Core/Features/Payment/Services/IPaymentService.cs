using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
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
        /// <param name="paymentConfiguration"></param>
        /// <returns></returns>
        Task<PaymentInformation> StartPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration);

        /// <summary>
        /// Cancel payment for an instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="paymentConfiguration"></param>
        /// <returns></returns>
        Task CancelPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration);

        /// <summary>
        /// Handle callback from payment provider.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="paymentConfiguration"></param>
        /// <param name="request"></param>
        Task HandleCallback(Instance instance, AltinnPaymentConfiguration paymentConfiguration, HttpRequest request);

        /// <summary>
        /// Handle return redirect from payment provider.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<string> HandleRedirect(Instance instance, HttpRequest request);

        // /// <summary>
        // /// Handles the cancel redirect from payment provider.
        // /// </summary>
        // /// <param name="request">The HTTP request.</param>
        // /// <returns>A task representing the asynchronous operation. The task result contains a string.</returns>
        // Task<string> HandleCancelRedirect(HttpRequest request);
    }
}
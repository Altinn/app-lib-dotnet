using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;

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
        /// Check updated payment information from payment provider and store the updated data.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="paymentConfiguration"></param>
        /// <returns></returns>
        Task<PaymentInformation?> CheckAndStorePaymentInformation(Instance instance, AltinnPaymentConfiguration paymentConfiguration);
        
        /// <summary>
        /// Cancel payment for an instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="paymentConfiguration"></param>
        /// <returns></returns>
        Task CancelPayment(Instance instance, AltinnPaymentConfiguration paymentConfiguration);
    }
}
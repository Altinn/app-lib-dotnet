using Altinn.App.Core.Features.Payment.Providers.Nets.Models;

namespace Altinn.App.Core.Features.Payment.Providers.Nets
{
    public interface INetsClient
    {
        Task<HttpApiResult<NetsPaymentSuccess>> CreatePayment(NetsCreatePayment payment);
        Task<HttpApiResult<NetsPaymentFull>> RetrievePayment(string paymentId);
    }
}
using Altinn.App.Core.Features.Payment.Providers.Nets;
using Altinn.App.Core.Features.Payment.Providers.Nets.Models;

namespace Altinn.App.Api.Tests.Mocks.Services
{
    public class NetsClientMock : INetsClient
    {
        public Task<HttpApiResult<NetsCreatePaymentSuccess>> CreatePayment(NetsCreatePayment payment)
        {
            throw new NotImplementedException();
        }

        public Task<HttpApiResult<NetsPaymentFull>> RetrievePayment(string paymentId)
        {
            throw new NotImplementedException();
        }

        public Task<HttpApiResult<bool>> CancelPayment(string paymentId)
        {
            throw new NotImplementedException();
        }
    }
}

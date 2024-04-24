using Altinn.App.Core.Features.Payment.Processors.Nets;
using Altinn.App.Core.Features.Payment.Processors.Nets.Models;

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

        public Task<bool> TerminatePayment(string paymentId)
        {
            throw new NotImplementedException();
        }
    }
}

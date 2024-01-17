using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Api.Tests.Mocks
{
    public class OrderDetailsFormatterMock : IOrderDetailsFormatter
    {
        public Task<OrderDetails> GetOrderDetails(Instance instance)
        {
            throw new NotImplementedException();
        }
    }
}

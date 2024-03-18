using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Api.Tests.Mocks
{
    public class OrderDetailsCalculatorMock : IOrderDetailsCalculator
    {
        public Task<OrderDetails> CalculateOrderDetails(Instance instance)
        {
            throw new NotImplementedException();
        }
    }
}

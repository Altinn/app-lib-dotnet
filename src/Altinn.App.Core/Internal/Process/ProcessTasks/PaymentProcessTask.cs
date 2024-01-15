using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for collecting user payment.
    /// </summary>
    public class PaymentProcessTask : IProcessTask
    {
        private readonly IOrderDetailsFormatter? _orderDetailsFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentProcessTask"/> class.
        /// </summary>
        /// <param name="orderDetailsFormatter"></param>
        public PaymentProcessTask(IOrderDetailsFormatter? orderDetailsFormatter = null)
        {
            _orderDetailsFormatter = orderDetailsFormatter;
        }

        /// <inheritdoc/>
        public string Type => "payment";

        /// <inheritdoc/>
        public async Task Start(string elementId, Instance instance, Dictionary<string, string> prefill)
        {
            if (_orderDetailsFormatter == null)
                throw new ProcessException("No IOrderDetailsFormatter implementation found. Implement the interface and add it as a transient service in Program.cs");

            PaymentOrder paymentOrder = await _orderDetailsFormatter.GetOrderDetails(instance);

            //TODO: Store payment order data somehow/somewhere, so that frontend can access it.
        }

        /// <inheritdoc/>
        public async Task End(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task Abandon(string elementId, Instance instance)
        {
            await Task.CompletedTask;
        }
    }
}

using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for collecting user payment.
    /// </summary>
    public class PaymentProcessTask : IProcessTask
    {
        private readonly IProcessReader _processReader;
        private readonly IPaymentService _paymentService;
        private readonly IOrderDetailsCalculator? _orderDetailsFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentProcessTask"/> class.
        /// </summary>
        /// <param name="orderDetailsFormatter"></param>
        /// <param name="processReader"></param>
        /// <param name="paymentService"></param>
        public PaymentProcessTask(IProcessReader processReader, IPaymentService paymentService, IOrderDetailsCalculator? orderDetailsFormatter = null)
        {
            _processReader = processReader;
            _paymentService = paymentService;
            _orderDetailsFormatter = orderDetailsFormatter;
        }

        /// <inheritdoc/>
        public string Type => "payment";

        /// <inheritdoc/>
        public async Task Start(string taskId, Instance instance, Dictionary<string, string> prefill)
        {
            if (_orderDetailsFormatter == null)
                throw new ProcessException(
                    "No IOrderDetailsFormatter implementation found for generating the order lines. Implement the interface and add it as a transient service in Program.cs");

            AltinnPaymentConfiguration? paymentConfiguration = _processReader.GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)?.PaymentConfiguration;
            if (paymentConfiguration == null)
                throw new PaymentException("PaymentConfiguration not found in AltinnTaskExtension");

            await _paymentService.StartPayment(instance, paymentConfiguration);
        }

        /// <inheritdoc/>
        public async Task End(string taskId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task Abandon(string taskId, Instance instance)
        {
            AltinnPaymentConfiguration? paymentConfiguration = _processReader.GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)?.PaymentConfiguration;
            if (paymentConfiguration == null)
                throw new PaymentException("PaymentConfiguration not found in AltinnTaskExtension");

            await _paymentService.CancelPayment(instance, paymentConfiguration);
        }
    }
}
using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Services;
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
        private readonly IOrderDetailsCalculator? _orderDetailsCalculator;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentProcessTask"/> class.
        /// </summary>
        /// <param name="processReader"></param>
        /// <param name="paymentService"></param>
        /// <param name="orderDetailsCalculator"></param>
        public PaymentProcessTask(IProcessReader processReader, IPaymentService paymentService,
            IOrderDetailsCalculator orderDetailsCalculator)
        {
            _processReader = processReader;
            _paymentService = paymentService;
            _orderDetailsCalculator = orderDetailsCalculator;
        }

        /// <inheritdoc/>
        public string Type => "payment";

        /// <inheritdoc/>
        public async Task Start(string taskId, Instance instance)
        {
            ArgumentNullException.ThrowIfNull(taskId);
            ArgumentNullException.ThrowIfNull(instance);

            AltinnPaymentConfiguration? paymentConfiguration = _processReader.GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)?.PaymentConfiguration;
            if (paymentConfiguration == null)
                throw new PaymentException("PaymentConfiguration not found in AltinnTaskExtension");

            await _paymentService.StartPayment(instance, paymentConfiguration);
        }

        /// <inheritdoc/>
        public async Task End(string taskId, Instance instance)
        {
            await Task.CompletedTask; //TODO: Lag en ITaskValidator som registreres hvis payment registreres.
        }

        /// <inheritdoc/>
        public async Task Abandon(string taskId, Instance instance)
        {
            ArgumentNullException.ThrowIfNull(taskId);
            ArgumentNullException.ThrowIfNull(instance);

            AltinnPaymentConfiguration? paymentConfiguration = _processReader.GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)?.PaymentConfiguration;
            if (paymentConfiguration == null)
                throw new PaymentException("PaymentConfiguration not found in AltinnTaskExtension");

            await _paymentService.CancelPaymentIfNotPaid(instance, paymentConfiguration);
        }
    }
}
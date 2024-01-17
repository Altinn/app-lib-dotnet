using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features.Payment;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using System.Text.Json;

namespace Altinn.App.Core.Internal.Process.ProcessTasks
{
    /// <summary>
    /// Represents the process task responsible for collecting user payment.
    /// </summary>
    public class PaymentProcessTask : IProcessTask
    {
        private readonly IProcessReader _processReader;
        private readonly IPaymentService _paymentService;
        private readonly IDataService _dataService;
        private readonly IOrderDetailsFormatter? _orderDetailsFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentProcessTask"/> class.
        /// </summary>
        /// <param name="orderDetailsFormatter"></param>
        /// <param name="processReader"></param>
        /// <param name="paymentService"></param>
        /// <param name="dataService"></param>
        public PaymentProcessTask(IProcessReader processReader, IPaymentService paymentService, IDataService dataService, IOrderDetailsFormatter? orderDetailsFormatter = null)
        {
            _processReader = processReader;
            _paymentService = paymentService;
            _dataService = dataService;
            _orderDetailsFormatter = orderDetailsFormatter;
        }

        /// <inheritdoc/>
        public string Type => "payment";

        /// <inheritdoc/>
        public async Task Start(string taskId, Instance instance, Dictionary<string, string> prefill)
        {
            if (_orderDetailsFormatter == null)
                throw new ProcessException("No IOrderDetailsFormatter implementation found for generating the order lines. Implement the interface and add it as a transient service in Program.cs");

            AltinnPaymentConfiguration? paymentConfiguration = GetPaymentConfiguration(GetCurrentTask(instance));
            string dataTypeId = paymentConfiguration.PaymentDataType;

            (Guid dataElementId, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

            if (paymentInformation != null && paymentInformation.Status != PaymentStatus.Paid)
            {
                await _paymentService.CancelPayment(instance, paymentInformation!);
                await _dataService.DeleteById(instance, dataElementId);
            }

            paymentInformation = await _paymentService.StartPayment(instance);
            await _dataService.InsertObjectAsJson(instance, dataTypeId, paymentInformation);
        }

        /// <inheritdoc/>
        public async Task End(string taskId, Instance instance)
        {
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task Abandon(string taskId, Instance instance)
        {
            AltinnPaymentConfiguration? paymentConfiguration = GetPaymentConfiguration(GetCurrentTask(instance));
            string dataTypeId = paymentConfiguration.PaymentDataType;

            (Guid dataElementId, PaymentInformation paymentInformation) = await _dataService.GetByType<PaymentInformation>(instance, dataTypeId);

            if (paymentInformation.Status == PaymentStatus.Paid)
                return;

            await _paymentService.CancelPayment(instance, paymentInformation);
            await _dataService.DeleteById(instance, dataElementId);
        }

        private ProcessTask GetCurrentTask(Instance instance)
        {
            if (_processReader.GetFlowElement(instance.Process.CurrentTask.ElementId) is ProcessTask currentTask)
            {
                return currentTask;
            }

            throw new ProcessException("Current task could not be cast to ProcessTask.");
        }

        private static AltinnPaymentConfiguration GetPaymentConfiguration(ProcessTask currentTask)
        {
            AltinnPaymentConfiguration? paymentConfiguration = currentTask.ExtensionElements?.TaskExtension?.PaymentConfiguration;
            if (paymentConfiguration == null)
                throw new ProcessException("No payment configuration found on payment process task. Add payment configuration to task.");

            return paymentConfiguration;
        }
    }
}

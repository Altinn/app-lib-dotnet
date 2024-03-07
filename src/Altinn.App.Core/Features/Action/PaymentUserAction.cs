using Altinn.App.Core.Features.Payment.Exceptions;
using Altinn.App.Core.Features.Payment.Models;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.UserAction;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Action
{
    /// <summary>
    /// User action for payment
    /// </summary>
    public class PaymentUserAction : IUserAction
    {
        private readonly IProcessReader _processReader;
        private readonly IDataService _dataService;
        private readonly ILogger<PaymentUserAction> _logger;
        private readonly IPaymentService _paymentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentUserAction"/> class
        /// </summary>
        /// <param name="processReader"></param>
        /// <param name="dataService"></param>
        /// <param name="paymentService"></param>
        /// <param name="logger"></param>
        public PaymentUserAction(IProcessReader processReader, IDataService dataService, IPaymentService paymentService, ILogger<PaymentUserAction> logger)
        {
            _processReader = processReader;
            _dataService = dataService;
            _paymentService = paymentService;
            _logger = logger;

        }

        /// <summary>
        /// Gets the id of the user action
        /// </summary>
        public string Id => "pay";

        /// <inheritdoc />
        public async Task<UserActionResult> HandleAction(UserActionContext context)
        {
            if (_processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) is not ProcessTask currentTask)
            {
                return UserActionResult.FailureResult(new ActionError()
                {
                    Code = "NoProcessTask",
                    Message = "Current task is not a process task."
                });
            }

            AltinnPaymentConfiguration? paymentConfiguration = currentTask.ExtensionElements?.TaskExtension?.PaymentConfiguration;
            if (paymentConfiguration == null)
                throw new PaymentException("No payment configuration found on payment process task. Add payment configuration to task.");

            if (paymentConfiguration.PaymentDataType == null)
                throw new PaymentException(
                    "No payment data type found on payment configuration for payment process task. Add payment data type to task config.");

            (_, PaymentInformation? paymentInformation) = await _dataService.GetByType<PaymentInformation>(context.Instance, paymentConfiguration.PaymentDataType);

            if (paymentInformation?.RedirectUrl == null)
            {
                throw new PaymentException("No redirect url found on payment information. Should have been added when payment process task was started.");
            }

            return UserActionResult.RedirectResult(paymentInformation.RedirectUrl);

        }
    }
}

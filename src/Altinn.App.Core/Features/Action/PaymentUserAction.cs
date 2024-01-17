using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.UserAction;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Action
{
    internal class PaymentUserAction : IUserAction
    {
        private readonly IProcessReader _processReader;
        private readonly IDataService _dataService;
        private readonly ILogger<PaymentUserAction> _logger;
        private readonly IPaymentService _paymentService;

        public PaymentUserAction(IProcessReader processReader, IDataService dataService, IPaymentService paymentService, ILogger<PaymentUserAction> logger)
        {
            _processReader = processReader;
            _dataService = dataService;
            _paymentService = paymentService;
            _logger = logger;

        }

        public string Id => "pay";

        public async Task<UserActionResult> HandleAction(UserActionContext context)
        {
            if (_processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) is ProcessTask currentTask)
            {
                AltinnPaymentConfiguration? paymentConfiguration = currentTask.ExtensionElements?.TaskExtension?.PaymentConfiguration;
                if (paymentConfiguration == null)
                    throw new ProcessException("No payment configuration found on payment process task. Add payment configuration to task.");

                (_, PaymentInformation paymentInformation) = await _dataService.GetByType<PaymentInformation>(context.Instance, paymentConfiguration.PaymentDataType);

                if (paymentInformation?.RedirectUrl == null)
                {
                    throw new ProcessException("No redirect url found on payment information. Should have been added when payment process task was started.");
                }

                return UserActionResult.RedirectResult(paymentInformation.RedirectUrl);
            }

            return UserActionResult.FailureResult(new ActionError()
            {
                Code = "NoProcessTask",
                Message = "Current task is not a process task."
            });
        }
    }
}

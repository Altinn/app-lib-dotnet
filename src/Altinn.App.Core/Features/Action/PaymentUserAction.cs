using Altinn.App.Core.Features.Payment.Providers;
using Altinn.App.Core.Features.Payment.Services;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models.UserAction;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Action
{
    internal class PaymentUserAction : IUserAction
    {
        private readonly IProcessReader _processReader;
        private readonly ILogger<PaymentUserAction> _logger;
        private readonly IPaymentService _paymentService;

        public PaymentUserAction(IProcessReader processReader, ILogger<PaymentUserAction> logger, IPaymentService paymentService)
        {
            _processReader = processReader;
            _logger = logger;
            _paymentService = paymentService;
        }

        public string Id => "payment";

        public async Task<UserActionResult> HandleAction(UserActionContext context)
        {
            if (_processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) is ProcessTask currentTask)
            {
                _logger.LogInformation("Payment action handler invoked for instance {Id}. In task: {CurrentTaskId}", context.Instance.Id, currentTask.Id);

                PaymentStartResult paymentStartResult = await _paymentService.StartPayment(context.Instance);

                return UserActionResult.SuccessResult();
            }

            return UserActionResult.FailureResult(new ActionError()
            {
                Code = "NoProcessTask",
                Message = "Current task is not a process task."
            });
        }
    }
}

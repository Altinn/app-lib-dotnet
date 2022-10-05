using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Altinn.Common.EFormidlingClient;
using Altinn.Common.EFormidlingClient.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Controllers
{
    /// <summary>
    /// Handles status checking of messages sent through the Eformidling integration point.
    /// </summary>
    public class EformidlingStatusCheckEventHandler : IEventHandler
    {
        private readonly IEFormidlingClient _eFormidlingClient;
        private readonly ILogger<EformidlingStatusCheckEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EformidlingStatusCheckEventHandler"/> class.
        /// </summary>
        public EformidlingStatusCheckEventHandler(IEFormidlingClient eFormidlingClient, ILogger<EformidlingStatusCheckEventHandler> logger)
        {
            _eFormidlingClient = eFormidlingClient;
            _logger = logger;
        }

        /// <inheritDoc/>
        public string EventType { get; internal set; } = "app.eformidling.reminder.checkstatus";

        /// <inheritDoc/>
        public async Task ProcessEvent(CloudEvent cloudEvent)
        {
            var subject = cloudEvent.Subject;

            _logger.LogInformation("Received reminder for subject {subject}", subject);

            var id = string.Empty; //TODO: where to get message id?
            var requestHeaders = new Dictionary<string, string>(); //TODO: Do we need any?

            Statuses statuses = await _eFormidlingClient.GetMessageStatusById(id, requestHeaders);

            _logger.LogInformation($"Received {statuses.Content.Count} statuses.");



            //TODO: Call eformidling integration point and checks status on message
            //TODO: Update status on instance if message is confirmed. Should wait in feedback step.
            //TODO: Throw exception or return success/failure, or resulttype? Rename to TryProcessEvent?
            //TODO: Dead messages in the Event system can still happen - these needs to be alerted, add alerts endpoint?
        }
    }
}
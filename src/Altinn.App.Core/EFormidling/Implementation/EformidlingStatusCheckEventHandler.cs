using Altinn.App.Core.Features;
using Altinn.App.Core.Models;

namespace Altinn.App.Controllers
{
    /// <summary>
    /// Handles status checking of messages sent through the Eformidling integration point.
    /// </summary>
    public class EformidlingStatusCheckEventHandler : IEventHandler
    {        
        /// <inheritDoc/>
        public string EventType { get; internal set; } = "app.eformidling.reminder.checkstatus";

        /// <inheritDoc/>
        public void ProcessEvent(CloudEvent cloudEvent)
        {
            //TODO: Call eformidling integration point and checks tatus on message
            //TODO: Update status on instance if message is confirmed. Should wait in feedback step.
            //TODO: Throw exception or return success/failure, or resulttype? Rename to TryProcessEvent?
            //TODO: Dead messages in the Event system can still happen - these needs to be alerted, add alerts endpoint?
        }
    }
}
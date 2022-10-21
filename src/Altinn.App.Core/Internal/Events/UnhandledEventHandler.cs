using Altinn.App.Core.Features;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.Events
{
    /// <summary>
    /// Implementation used to handled events that could not bee resolved and matched on type.
    /// </summary>
    public class UnhandledEventHandler : IEventHandler
    {
        /// <inheritdoc/>
        public string EventType => "app.events.unhandled";

        /// <inheritdoc/>
        public Task<bool> ProcessEvent(CloudEvent cloudEvent)
        {
            //TODO: Log the event data and throw exception
            return Task.FromResult(false); ;
        }
    }
}
using System;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Events;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers
{
    /// <summary>
    /// Controller for handling inbound events from the event system
    /// </summary>
    [Route("{org}/{app}/api/v1/eventsreceiver")]
    public class EventsReceiverController : ControllerBase
    {
        private readonly IEventHandlerResolver _eventHandlerResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventsReceiverController"/> class.
        /// </summary>
        public EventsReceiverController(IEventHandlerResolver eventHandlerResolver)
        {
            _eventHandlerResolver = eventHandlerResolver;
        }

        /// <summary>
        /// Create a new inbound for the app to process.
        /// </summary>
        [HttpPost]
        public ActionResult Post([FromBody] CloudEvent cloudEvent)
        {
            IEventHandler eventHandler = _eventHandlerResolver.ResolveEventHandler(cloudEvent.Type);

            try
            {
                eventHandler.ProcessEvent(cloudEvent);
                return Ok();
            }
            catch (Exception ex)
            {
                return new StatusCodeResult(425);
            }
        }
    }
}

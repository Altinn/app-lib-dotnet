using Altinn.App.Core.EFormidling.Implementation;
using Altinn.App.Core.Features;
using Altinn.App.Core.Interface;
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
        private readonly IInstance _instanceClient;
        private readonly ILogger<EformidlingStatusCheckEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EformidlingStatusCheckEventHandler"/> class.
        /// </summary>
        public EformidlingStatusCheckEventHandler(IEFormidlingClient eFormidlingClient, IInstance instanceClient, ILogger<EformidlingStatusCheckEventHandler> logger)
        {
            _eFormidlingClient = eFormidlingClient;
            _instanceClient = instanceClient;
            _logger = logger;
        }

        /// <inheritDoc/>
        public string EventType { get; internal set; } = "app.eformidling.reminder.checkinstancestatus";

        /// <inheritDoc/>
        public async Task<bool> ProcessEvent(CloudEvent cloudEvent)
        {
            var subject = cloudEvent.Subject;

            _logger.LogInformation("Received reminder for subject {subject}", subject);

            InstanceIdentifier instanceIdentifier = InstanceIdentifier.CreateFromUrl(cloudEvent.Source.ToString());
            
            // Instance GUID is used as shipment identifier
            string id = instanceIdentifier.InstanceGuid.ToString();
            Statuses statusesForShipment = await GetStatusesForShipment(id);
            if (MessageDeliveredToKS(statusesForShipment))
            {
                // Update status on instance if message is confirmed delivered to KS.
                // The instance should wait in feedback step. This enforces a feedback step in the process in current version.
                // Moving forward sending to Eformidling should considered as a ServiceTask with auto advance in the process
                // when the message is confirmed.                
                _ = await _instanceClient.AddCompleteConfirmation(instanceIdentifier.InstanceOwnerPartyId, instanceIdentifier.InstanceGuid);

                return true;
            }
            else if (MessageMalformed(statusesForShipment, out string errorMalformed))
            {
                throw new EformidlingDeliveryException($"The message with id {id} was not delivered by Eformidling to KS. Error from Eformidling: {errorMalformed}.");
            }
            else if (MessageTimedOutToKS(statusesForShipment, out string errorTimeout))
            {
                throw new EformidlingDeliveryException($"The message with id {id} was not delivered by Eformidling to KS. The message lifetime has expired. Error from Eformidling: {errorTimeout}");
            }
            else
            {
                // The message isn't processed yet.
                // We will try again later.
                return false;
            }
                        
            // We don't know if this is the last reminder from the Event system. If the
            // Event system gives up (after 48 hours) it will end up in the dead letter queue,
            // and be handled by the Platform team manually.
        }

        private async Task<Statuses> GetStatusesForShipment(string shipmentId)
        {
            var requestHeaders = new Dictionary<string, string>(); //TODO: Do we need any? Probably Authorization headers

            Statuses statuses = await _eFormidlingClient.GetMessageStatusById(shipmentId, requestHeaders);

            _logger.LogInformation("Received the following {count} statuses: {statusValues}.", statuses.Content.Count, string.Join(",", statuses.Content.Select(s => s.Status).ToArray()));

            return statuses;
        }

        private static bool MessageDeliveredToKS(Statuses statuses)
        {
            return statuses.Content.FirstOrDefault(s => s.Status.ToLower() == "levert") != null;
        }

        private static bool MessageTimedOutToKS(Statuses statuses, out string errorMessage)
        {
            (bool error, errorMessage) = CheckErrorStatus(statuses, "levetid_utlopt");
            return error;
        }

        private static bool MessageMalformed(Statuses statuses, out string errorMessage)
        {
            (bool error, errorMessage) = CheckErrorStatus(statuses, "feil");
            return error;
        }

        private static (bool Error, string ErrorMessage) CheckErrorStatus(Statuses statuses, string errorStatus)
        {
            bool isError = false;
            string errorMessage = string.Empty;

            var status = statuses.Content.FirstOrDefault(s => s.Status.ToLower() == errorStatus);
            if (status != null)
            {
                isError = true;
                errorMessage = status.Description;
            }

            return (isError, errorMessage);
        }
    }
}
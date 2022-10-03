using Altinn.App.Core.Infrastructure.Clients.Events;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.EFormidling
{
    /// <summary>
    /// Hosted service to set up prequisites for Eformidling integration.
    /// </summary>
    public class EformidlingStartup : IHostedService
    {
        private readonly AppIdentifier _appIdentifier;
        private readonly IEvents _eventsClient;
        private readonly ILogger<EformidlingStartup> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EformidlingStartup"/> class.
        /// </summary>
        public EformidlingStartup(AppIdentifier appId, IEvents eventsClient, ILogger<EformidlingStartup> logger)
        {
            _appIdentifier = appId;
            _eventsClient = eventsClient;
            _logger = logger;
        }

        ///<inheritDoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var eventType = "app.eformidling.reminder.checkstatus";
            try
            {
                Subscription subscription = await _eventsClient.AddSubscription(_appIdentifier.Org, _appIdentifier.App, eventType);

                _logger.LogInformation($"Successfully subscribed to event {eventType} for app {_appIdentifier}. Subscription {subscription.Id} is being used.");
            }

            catch
            {
                _logger.LogError($"Unable to subscribe to event {eventType} for app {_appIdentifier}");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

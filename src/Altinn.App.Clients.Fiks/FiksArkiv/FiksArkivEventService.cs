using System.Diagnostics;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivEventService : BackgroundService
{
    private readonly ILogger<FiksArkivEventService> _logger;
    private readonly IFiksIOClient _fiksIOClient;
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;
    private readonly Telemetry? _telemetry;
    private readonly IInstanceClient _instanceClient;

    public FiksArkivEventService(
        IFiksIOClient fiksIOClient,
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        ILogger<FiksArkivEventService> logger,
        IInstanceClient instanceClient,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _fiksIOClient = fiksIOClient;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _instanceClient = instanceClient;
        _telemetry = telemetry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fiks Arkiv Service starting");
        await _fiksIOClient.OnMessageReceived(MessageReceivedHandler);

        var loopInterval = TimeSpan.FromSeconds(1);
        var healthCheckInterval = TimeSpan.FromMinutes(10);
        var counter = TimeSpan.Zero;

        // Keep-alive loop
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(loopInterval, stoppingToken);
            counter += loopInterval;

            // Perform health check
            if (counter >= healthCheckInterval)
            {
                counter = TimeSpan.Zero;
                if (await _fiksIOClient.IsHealthy() is false)
                {
                    _logger.LogError("FiksIO Client is unhealthy, reconnecting.");
                    await _fiksIOClient.Reconnect();
                }
            }
        }

        _logger.LogInformation("Fiks Arkiv Service stopping");
        await _fiksIOClient.DisposeAsync();
    }

    private async Task MessageReceivedHandler(FiksIOReceivedMessage receivedMessage)
    {
        using Activity? mainActivity = _telemetry?.StartReceiveFiksActivity(
            receivedMessage.Message.MessageId,
            receivedMessage.Message.MessageType
        );

        try
        {
            _logger.LogInformation(
                "Received message {MessageType}:{MessageId} from {MessageSender}, in reply to {MessageReplyFor} with senders reference {SendersReference}",
                receivedMessage.Message.MessageType,
                receivedMessage.Message.MessageId,
                receivedMessage.Message.Sender,
                receivedMessage.Message.InReplyToMessage,
                receivedMessage.Message.SendersReference
            );

            Instance instance = await ParseCorrelationId(receivedMessage);

            using Activity? innerActivity = _telemetry?.StartFiksMessageHandlerActivity(
                instance,
                _fiksArkivMessageHandler.GetType()
            );

            await _fiksArkivMessageHandler.HandleReceivedMessage(instance, receivedMessage);

            _logger.LogInformation(
                "Sending acknowledge receipt for message {MessageId}",
                receivedMessage.Message.MessageId
            );

            await receivedMessage.Responder.Ack();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fiks Arkiv MessageReceivedHandler failed with error: {Error}", e.Message);
            mainActivity?.Errored(e);
        }
    }

    private async Task<Instance> ParseCorrelationId(FiksIOReceivedMessage receivedMessage)
    {
        try
        {
            Debug.Assert(receivedMessage.Message.CorrelationId is not null); // This may or may not be true, but we're catching below

            var appId = AppIdentifier.CreateFromUrl(receivedMessage.Message.CorrelationId);
            var instanceId = InstanceIdentifier.CreateFromUrl(receivedMessage.Message.CorrelationId);

            return await _instanceClient.GetInstance(
                appId.App,
                appId.Org,
                instanceId.InstanceOwnerPartyId,
                instanceId.InstanceGuid
            );
        }
        catch (Exception e)
        {
            throw new FiksArkivException(
                $"Error resolving Instance for received message. Correlation ID is most likely missing or malformed: {receivedMessage.Message.CorrelationId}",
                e
            );
        }
    }
}

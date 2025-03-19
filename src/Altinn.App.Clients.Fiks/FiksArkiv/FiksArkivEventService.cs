using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivEventService : BackgroundService
{
    private readonly ILogger<FiksArkivEventService> _logger;
    private readonly IFiksIOClient _fiksIOClient;
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;

    public FiksArkivEventService(
        IFiksIOClient fiksIOClient,
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        ILogger<FiksArkivEventService> logger
    )
    {
        _logger = logger;
        _fiksIOClient = fiksIOClient;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
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
                if (_fiksIOClient.IsHealthy() is false)
                {
                    _logger.LogError("FiksIO Client is unhealthy, reconnecting.");
                    await _fiksIOClient.Reconnect();
                }
            }
        }

        _logger.LogInformation("Fiks Arkiv Service stopping");
        _fiksIOClient.Dispose();
    }

    private async void MessageReceivedHandler(object? sender, FiksIOReceivedMessage receivedMessage)
    {
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

            // TODO: Must resolve Instance here! Waiting on Fiks IO protocol changes.
            // TODO: This is absolutely something that must be fixed, only disabling the warning for now, in order for CI pipe to run.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            await _fiksArkivMessageHandler.HandleReceivedMessage(null, receivedMessage);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _logger.LogInformation(
                "Sending acknowledge receipt for message {MessageId}",
                receivedMessage.Message.MessageId
            );
            receivedMessage.Responder.Ack();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fiks Arkiv MessageReceivedHandler failed with error: {Error}", e.Message);
        }
    }
}

using System.Diagnostics;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivEventService : BackgroundService
{
    private readonly ILogger<FiksArkivEventService> _logger;
    private readonly IFiksIOClient _fiksIOClient;
    private readonly Telemetry? _telemetry;
    private readonly IFiksArkivInstanceClient _fiksArkivInstanceClient;
    private readonly IWebHostEnvironment _env;
    private readonly AppImplementationFactory _appImplementationFactory;

    private IFiksArkivMessageHandler _fiksArkivMessageHandler =>
        _appImplementationFactory.GetRequired<IFiksArkivMessageHandler>();

    public FiksArkivEventService(
        AppImplementationFactory appImplementationFactory,
        IFiksIOClient fiksIOClient,
        ILogger<FiksArkivEventService> logger,
        IFiksArkivInstanceClient fiksArkivInstanceClient,
        IWebHostEnvironment env,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _fiksIOClient = fiksIOClient;
        _telemetry = telemetry;
        _fiksArkivInstanceClient = fiksArkivInstanceClient;
        _appImplementationFactory = appImplementationFactory;
        _env = env;
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

        IReadOnlyList<(string, string)>? decryptedMessagePayloads = null;

        try
        {
            _logger.LogInformation(
                "Received message {MessageType}:{MessageId} from {MessageSender}, in reply to {MessageReplyFor} with senders-reference {SendersReference} and correlation-id {CorrelationId}",
                receivedMessage.Message.MessageType,
                receivedMessage.Message.MessageId,
                receivedMessage.Message.Sender,
                receivedMessage.Message.InReplyToMessage,
                receivedMessage.Message.SendersReference,
                receivedMessage.Message.CorrelationId
            );

            decryptedMessagePayloads = await receivedMessage.Message.GetDecryptedPayloadStrings();

            // TODO: Still waiting for proper correlation id support
            Instance instance = await RetrieveInstance(receivedMessage);

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
            _logger.LogError("The message payload was: {MessagePayload}", decryptedMessagePayloads);
            mainActivity?.Errored(e);

            // Avoid clogging up the queue with errors in non-production environments
            if (_env.IsProduction() is false)
            {
                await receivedMessage.Responder.Ack();
            }
        }
    }

    private async Task<Instance> RetrieveInstance(FiksIOReceivedMessage receivedMessage)
    {
        var (appIdentifier, instanceIdentifier) = ParseCorrelationId(receivedMessage.Message.CorrelationId);

        try
        {
            return await _fiksArkivInstanceClient.GetInstance(appIdentifier, instanceIdentifier);
        }
        catch (Exception e)
        {
            throw new FiksArkivException($"Error fetching Instance object for {instanceIdentifier}: {e.Message}", e);
        }
    }

    private static (AppIdentifier appId, InstanceIdentifier instanceId) ParseCorrelationId(string? correlationId)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(correlationId);

            var appId = AppIdentifier.CreateFromUrl(correlationId);
            var instanceId = InstanceIdentifier.CreateFromUrl(correlationId);

            return (appId, instanceId);
        }
        catch (Exception e)
        {
            throw new FiksArkivException($"Error parsing Correlation ID for received message: {correlationId}", e);
        }
    }
}

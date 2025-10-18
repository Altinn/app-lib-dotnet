using System.Diagnostics;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivEventService : BackgroundService
{
    private readonly ILogger<FiksArkivEventService> _logger;
    private readonly IFiksIOClient _fiksIOClient;
    private readonly Telemetry? _telemetry;
    private readonly IFiksArkivInstanceClient _fiksArkivInstanceClient;
    private readonly IFiksArkivMessageHandler _fiksArkivMessageHandler;
    private readonly IHostEnvironment _env;
    private readonly TimeProvider _timeProvider;
    private readonly FiksArkivSettings _fiksArkivSettings;

    public FiksArkivEventService(
        IFiksArkivMessageHandler fiksArkivMessageHandler,
        IFiksIOClient fiksIOClient,
        IOptions<FiksArkivSettings> fiksArkivSettings,
        ILogger<FiksArkivEventService> logger,
        IFiksArkivInstanceClient fiksArkivInstanceClient,
        IHostEnvironment env,
        TimeProvider? timeProvider = null,
        Telemetry? telemetry = null
    )
    {
        _logger = logger;
        _fiksIOClient = fiksIOClient;
        _telemetry = telemetry;
        _fiksArkivSettings = fiksArkivSettings.Value;
        _fiksArkivInstanceClient = fiksArkivInstanceClient;
        _fiksArkivMessageHandler = fiksArkivMessageHandler;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _env = env;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Fiks Arkiv Service starting");
            await _fiksIOClient.OnMessageReceived(MessageReceivedHandler);

            DateTimeOffset nextIteration = GetLoopDelay();
            DateTimeOffset nextHealthCheck = GetHealthCheckDelay();

            // Keep-alive loop
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan delta = nextIteration - _timeProvider.GetUtcNow();
                await _timeProvider.Delay(delta > TimeSpan.Zero ? delta : TimeSpan.Zero, stoppingToken);

                // Perform health check
                if (_timeProvider.GetUtcNow() >= nextHealthCheck)
                {
                    if (await _fiksIOClient.IsHealthy() is false)
                    {
                        _logger.LogError("FiksIO Client is unhealthy, reconnecting.");
                        await _fiksIOClient.Reconnect();
                    }

                    nextHealthCheck = GetHealthCheckDelay();
                }

                nextIteration = GetLoopDelay();
            }
        }
        finally
        {
            _logger.LogInformation("Fiks Arkiv Service stopping.");
            await _fiksIOClient.DisposeAsync();
        }

        return;

        DateTimeOffset GetLoopDelay() => _timeProvider.GetUtcNow() + TimeSpan.FromSeconds(1);
        DateTimeOffset GetHealthCheckDelay() => _timeProvider.GetUtcNow() + TimeSpan.FromMinutes(10);
    }

    internal async Task MessageReceivedHandler(FiksIOReceivedMessage receivedMessage)
    {
        using Activity? mainActivity = _telemetry?.StartReceiveFiksActivity(
            receivedMessage.Message.MessageId,
            receivedMessage.Message.MessageType
        );

        Instance? instance = null;

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

            instance = await RetrieveInstance(receivedMessage);

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

            var decryptedMessagePayloads = await TryGetDecryptedPayloads(receivedMessage);
            _logger.LogError("The message payload was: {MessagePayload}", decryptedMessagePayloads);

            // Avoid clogging up the queue with errors in non-production environments
            if (_env.IsProduction() is false)
            {
                await receivedMessage.Responder.Ack();
            }

            // Move the instance process forward if we are able
            if (instance is null)
            {
                _logger.LogError(
                    "Unable to move the process forward, because the `instance` object has not been resolved"
                );
            }
            else if (_fiksArkivSettings.AutoSend?.ErrorHandling?.MoveToNextTask is true)
            {
                await _fiksArkivInstanceClient.ProcessMoveNext(
                    new InstanceIdentifier(instance),
                    _fiksArkivSettings.AutoSend?.ErrorHandling?.Action
                );
            }
        }
    }

    private async Task<Instance> RetrieveInstance(FiksIOReceivedMessage receivedMessage)
    {
        InstanceIdentifier instanceIdentifier = ParseCorrelationId(receivedMessage.Message.CorrelationId);

        try
        {
            return await _fiksArkivInstanceClient.GetInstance(instanceIdentifier);
        }
        catch (Exception e)
        {
            throw new FiksArkivException($"Error fetching Instance object for {instanceIdentifier}: {e.Message}", e);
        }
    }

    private static InstanceIdentifier ParseCorrelationId(string? correlationId)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(correlationId);
            return InstanceIdentifier.CreateFromUrl(correlationId);
        }
        catch (Exception e)
        {
            throw new FiksArkivException($"Error parsing Correlation ID for received message: {correlationId}", e);
        }
    }

    private static async Task<IReadOnlyList<(string Filename, string Content)>?> TryGetDecryptedPayloads(
        FiksIOReceivedMessage receivedMessage
    )
    {
        try
        {
            return await receivedMessage.Message.GetDecryptedPayloads();
        }
        catch
        {
            return null;
        }
    }
}

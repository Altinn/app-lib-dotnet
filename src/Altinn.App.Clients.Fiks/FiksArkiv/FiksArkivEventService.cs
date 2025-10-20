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

    internal async Task MessageReceivedHandler(FiksIOReceivedMessage message)
    {
        using Activity? mainActivity = _telemetry?.StartReceiveFiksActivity(
            message.Message.MessageId,
            message.Message.MessageType
        );

        Instance? instance = null;

        try
        {
            _logger.LogInformation(
                "Received message {MessageType}:{MessageId} from {MessageSender}, in reply to {MessageReplyFor} with senders-reference {SendersReference} and correlation-id {CorrelationId}",
                message.Message.MessageType,
                message.Message.MessageId,
                message.Message.Sender,
                message.Message.InReplyToMessage,
                message.Message.SendersReference,
                message.Message.CorrelationId
            );

            instance = await RetrieveInstance(message);

            using Activity? innerActivity = _telemetry?.StartFiksMessageHandlerActivity(
                instance,
                _fiksArkivMessageHandler.GetType()
            );

            await _fiksArkivMessageHandler.HandleReceivedMessage(instance, message);
            await message.Responder.Ack();

            _logger.LogInformation(
                "Processing completed successfully for message {MessageId}",
                message.Message.MessageId
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fiks Arkiv MessageReceivedHandler failed with error: {Error}", e.Message);
            mainActivity?.Errored(e);

            // Don't ack messages we failed to process in PROD. Let Fiks IO redelivery and/or alarms.
            if (!_env.IsProduction())
                await message.Responder.Ack();

            await TryMoveProcessOnError(instance);
        }
    }

    private async Task TryMoveProcessOnError(Instance? instance)
    {
        if (instance is null)
        {
            _logger.LogError("Unable to move the process forward, because the `instance` object has not been resolved");
            return;
        }

        if (_fiksArkivSettings.AutoSend?.ErrorHandling is null)
        {
            _logger.LogWarning(
                "Unable to move the process forward, because the `FiksArkivSettings.AutoSend.ErrorHandling` configuration property has not been set"
            );
            return;
        }

        if (_fiksArkivSettings.AutoSend.ErrorHandling?.MoveToNextTask is true)
        {
            _logger.LogInformation(
                "`FiksArkivSettings.AutoSendErrorHandling.MoveToNextTask` has been disabled, taking no action."
            );
            return;
        }

        await _fiksArkivInstanceClient.ProcessMoveNext(
            new InstanceIdentifier(instance),
            _fiksArkivSettings.AutoSend?.ErrorHandling?.Action
        );
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
}

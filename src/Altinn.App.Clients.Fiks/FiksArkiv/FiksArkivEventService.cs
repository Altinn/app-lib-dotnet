using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.ASiC_E;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivEventService : BackgroundService
{
    private readonly ILogger<FiksArkivEventService> _logger;
    private readonly IFiksIOClient _fiksIOClient;

    // private readonly IFiksArkivErrorHandler _errorHandler;

    public FiksArkivEventService(
        IFiksIOClient fiksIOClient,
        // IFiksArkivErrorHandler errorHandler,
        ILogger<FiksArkivEventService> logger
    )
    {
        _logger = logger;
        _fiksIOClient = fiksIOClient;
        // _errorHandler = errorHandler;
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

    private async void MessageReceivedHandler(object? sender, FiksIOReceivedMessageArgs receivedMessage)
    {
        try
        {
            Guid messageId = receivedMessage.Message.MessageId;
            string messageType = receivedMessage.Message.MessageType;
            _logger.LogInformation(
                "Received message {MessageType}:{MessageId} in reply to {MessageReplyFor}",
                messageType,
                messageId,
                receivedMessage.Message.InReplyToMessage
            );

            IEnumerable<string> decryptedMessages;
            try
            {
                decryptedMessages = await GetDecryptedPayloads(receivedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting message content: {Exception}", ex.Message);
            }

            // `decryptedMessages` Could be `MappeKvittering` og `FeilmeldingBase` ... or unknown
            // TODO: Deserialize?

            if (string.IsNullOrWhiteSpace(messageType) || FiksArkivMeldingtype.IsFeilmelding(messageType))
            {
                _logger.LogError(
                    "Message {MessageType}:{MessageId} is an error reply. Executing error handler",
                    messageType,
                    messageId
                );

                // TODO: Retrieve instance and execute handler
                // var dummyInstance = new Instance
                // {
                //     Id = "501337/1b899c5b-2505-424e-be06-12cf36da7d1e",
                //     AppId = "ttd/fiks-arkiv-test",
                // };
                //
                // await _errorHandler.HandleError(dummyInstance, receivedMessage);
            }

            // TODO: Send /complete notification?
            receivedMessage.Responder.Ack();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "FiksArkiv MessageReceivedHandler failed with error: {Error}", e.Message);
            // receivedMessage.Responder.NackWithRequeue();
        }
    }

    private async Task<IEnumerable<string>> GetDecryptedPayloads(FiksIOReceivedMessageArgs receivedMessageArgs)
    {
        List<string> payloads = [];
        AsiceReader asiceReader = new();
        using var asiceReadModel = asiceReader.Read(await receivedMessageArgs.Message.DecryptedStream);

        foreach (var asiceVerifyReadEntry in asiceReadModel.Entries)
        {
            await using (var entryStream = asiceVerifyReadEntry.OpenStream())
            {
                _logger.LogInformation($"GetDecryptedPayloadTxt - {asiceVerifyReadEntry.FileName}");
                // await using var fileStream = new FileStream(
                //     $"received-{asiceVerifyReadEntry.FileName}",
                //     FileMode.Create,
                //     FileAccess.Write
                // );
                // await entryStream.CopyToAsync(fileStream);

                using var reader = new StreamReader(entryStream);
                string text = await reader.ReadToEndAsync();
                payloads.Add(text);
            }
        }

        return payloads;
    }
}

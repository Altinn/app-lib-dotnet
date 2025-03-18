using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Client.Send;
using KS.Fiks.IO.Crypto.Models;

namespace Altinn.App.Clients.Fiks.FiksIO.Models;

public sealed record FiksIOReceivedMessage
{
    public FiksIOReceivedMessageContent Message { get; init; }
    public FiksIOMessageResponder Responder { get; init; }

    public bool IsErrorResponse =>
        string.IsNullOrWhiteSpace(Message.MessageType) || Message.MessageType.Contains(FiksIOConstants.ErrorStub);

    internal FiksIOReceivedMessage(MottattMeldingArgs mottattMeldingArgs)
    {
        Message = new FiksIOReceivedMessageContent(mottattMeldingArgs.Melding);
        Responder = new FiksIOMessageResponder(mottattMeldingArgs.SvarSender);
    }
}

public sealed record FiksIOReceivedMessageContent
{
    public bool HasPayload => _mottattMelding.HasPayload;
    public Guid? InReplyToMessage => _mottattMelding.SvarPaMelding;
    public Task<Stream> EncryptedStream => _mottattMelding.EncryptedStream;
    public Task<Stream> DecryptedStream => _mottattMelding.DecryptedStream;
    public Task<IEnumerable<FiksIOMessagePayload>> DecryptedPayloads => DecryptedPayloadsWrapper();

    public Guid MessageId => _mottattMelding.MeldingId;
    public Guid? SendersReference => _mottattMelding.KlientMeldingId;
    public string MessageType => _mottattMelding.MeldingType;
    public Guid Sender => _mottattMelding.AvsenderKontoId;
    public Guid Recipient => _mottattMelding.MottakerKontoId;
    public TimeSpan MessageLifetime => _mottattMelding.Ttl;
    public Dictionary<string, string> Headers => _mottattMelding.Headere;
    public bool Resent => _mottattMelding.Resendt;

    public Task WriteEncryptedZip(string outPath) => _mottattMelding.WriteEncryptedZip(outPath);

    public Task WriteDecryptedZip(string outPath) => _mottattMelding.WriteDecryptedZip(outPath);

    private IMottattMelding _mottattMelding { get; init; }

    internal FiksIOReceivedMessageContent(IMottattMelding mottattMelding)
    {
        _mottattMelding = mottattMelding;
    }

    private async Task<IEnumerable<FiksIOMessagePayload>> DecryptedPayloadsWrapper()
    {
        var decryptedPayloads = await _mottattMelding.DecryptedPayloads;
        return decryptedPayloads.Select(x => new FiksIOMessagePayload(x.Filename, x.Payload));
    }
}

public sealed record FiksIOMessageResponder
{
    public Task<SendtMelding> Respond(string messageType, IList<IPayload> payloads, Guid? sendersReference = null) =>
        _svarSender.Svar(messageType, payloads, sendersReference);

    public Task<SendtMelding> Respond(
        string meldingType,
        Stream melding,
        string filename,
        Guid? sendersReference = null
    ) => _svarSender.Svar(meldingType, melding, filename, sendersReference);

    public Task<SendtMelding> Respond(
        string meldingType,
        string melding,
        string filename,
        Guid? sendersReference = null
    ) => _svarSender.Svar(meldingType, melding, filename, sendersReference);

    public Task<SendtMelding> Respond(string meldingType, string fileLocation, Guid? sendersReference = null) =>
        _svarSender.Svar(meldingType, fileLocation, sendersReference);

    public Task<SendtMelding> Respond(string meldingType, Guid? sendersReference = null) =>
        _svarSender.Svar(meldingType, sendersReference);

    public void Ack() => _svarSender.Ack();

    public void Nack() => _svarSender.Nack();

    public void NackWithRequeue() => _svarSender.NackWithRequeue();

    private ISvarSender _svarSender { get; init; }

    internal FiksIOMessageResponder(ISvarSender svarSender)
    {
        _svarSender = svarSender;
    }
}

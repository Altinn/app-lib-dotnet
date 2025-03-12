using KS.Fiks.IO.Client.Models;

namespace Altinn.App.Clients.Fiks.FiksIO.Models;

public sealed record FiksIOMessageResponse
{
    public Guid MessageId => _sendtMelding.MeldingId;
    public Guid? SendersReference => _sendtMelding.KlientMeldingId;
    public string MessageType => _sendtMelding.MeldingType;
    public Guid Sender => _sendtMelding.AvsenderKontoId;
    public Guid Recipient => _sendtMelding.MottakerKontoId;
    public TimeSpan MessageLifetime => _sendtMelding.Ttl;
    public Dictionary<string, string> Headers => _sendtMelding.Headere;
    public Guid? InReplyToMessage => _sendtMelding.SvarPaMelding;
    public bool Resendt => _sendtMelding.Resendt;

    private SendtMelding _sendtMelding { get; init; }

    internal FiksIOMessageResponse(SendtMelding sendtMelding)
    {
        _sendtMelding = sendtMelding;
    }
}

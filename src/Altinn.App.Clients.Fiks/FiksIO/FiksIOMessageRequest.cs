using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Crypto.Models;

namespace Altinn.App.Clients.Fiks.FiksIO;

public sealed record FiksIOMessageRequest(
    Guid Recipient,
    string MessageType,
    Guid SendersReference,
    IEnumerable<FiksIOMessagePayload> Payload,
    Guid? InReplyToMessage = null,
    TimeSpan? MessageLifetime = null,
    Dictionary<string, string>? Headers = null
)
{
    internal MeldingRequest ToMeldingRequest(Guid sender)
    {
        return new MeldingRequest(
            avsenderKontoId: sender,
            mottakerKontoId: Recipient,
            meldingType: MessageType,
            ttl: MessageLifetime,
            headere: Headers,
            svarPaMelding: InReplyToMessage,
            klientMeldingId: SendersReference
        );
    }

    internal IList<IPayload> ToPayload()
    {
        return Payload.Select(a => a.ToPayload()).ToList();
    }
}

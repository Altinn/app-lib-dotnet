using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Crypto.Models;

namespace Altinn.App.Clients.Fiks.FiksIO.Models;

/// <summary>
/// Represents a Fiks IO message request (outbound).
/// </summary>
/// <param name="Recipient">The intended recipient of the message (Fiks account ID).</param>
/// <param name="MessageType">The message type (e.g. `no.ks.fiks.arkiv.v1.arkivering.arkivmelding.opprett`).</param>
/// <param name="SendersReference">Your reference for this message.</param>
/// <param name="Payload">The payload(s) for the message.</param>
/// <param name="InReplyToMessage">Is this a reply? If so, the original message ID goes here.</param>
/// <param name="MessageLifetime">How long is this message valid for, if it goes unreceived?</param>
/// <param name="Headers">Message headers.</param>
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

using System.Text.Json.Serialization;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

/// <summary>
/// A JSON-serializable representation of a received Fiks Arkiv message,
/// containing the minimum data needed to fully process the message at a later time.
/// </summary>
public sealed record StoredFiksArkivMessage
{
    /// <summary>
    /// The Fiks IO message ID.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required Guid MessageId { get; init; }

    /// <summary>
    /// The Fiks Arkiv message type (e.g. <c>no.ks.fiks.arkiv.v1.arkivering.arkivmelding.opprett.kvittering</c>).
    /// </summary>
    [JsonPropertyName("messageType")]
    public required string MessageType { get; init; }

    /// <summary>
    /// The decrypted payloads attached to the message.
    /// </summary>
    [JsonPropertyName("payloads")]
    public IReadOnlyList<StoredFiksArkivPayload>? Payloads { get; init; }
}

/// <summary>
/// A single decrypted payload entry from a Fiks Arkiv message.
/// </summary>
public sealed record StoredFiksArkivPayload
{
    /// <summary>
    /// The filename of the payload (e.g. <c>arkivmelding.xml</c>).
    /// </summary>
    [JsonPropertyName("filename")]
    public required string Filename { get; init; }

    /// <summary>
    /// The decrypted XML content of the payload as a string.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

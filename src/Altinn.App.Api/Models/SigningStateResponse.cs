using System.Text.Json.Serialization;

namespace Altinn.App.Api.Models;

/// <summary>
/// Contains the result of a get signees request.
/// </summary>
public class SigningStateResponseDTO
{
    /// <summary>
    /// The signees for the current task.
    /// </summary>
    public required List<SigneeStateDTO> SigneeStates { get; set; }
}

/// <summary>
/// Contains information about a signee and the current signing status.
/// </summary>
public class SigneeStateDTO
{
    /// <summary>
    /// The name of the signee.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The organisation of the signee.
    /// </summary>
    [JsonPropertyName("organisation")]
    public string? Organisation { get; set; }

    /// <summary>
    /// Whether delegation of signing rights has been successful.
    /// </summary>
    [JsonPropertyName("delegationSuccessful")]
    public bool DelegationSuccessful { get; set; }

    /// <summary>
    /// Whether the signee has been notified to sign via email or sms.
    /// </summary>
    [JsonPropertyName("notificationSuccessful")]
    public NotificationState NotificationSuccessful { get; set; }

    /// <summary>
    /// The party id of the signee.
    /// </summary>
    [JsonPropertyName("partyId")]
    public required int PartyId { get; set; }

    /// <summary>
    /// The time the signee signed.
    /// </summary>
    [JsonPropertyName("signedTime")]
    public DateTime? SignedTime { get; set; }
}

/// <summary>
/// Represents the state of a notification.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NotificationState>))]
public enum NotificationState
{
    /// <summary>
    /// Notification has not been configures and thus has not been sent.
    /// </summary>
    NotSent,

    /// <summary>
    /// The notification has been sent successfully.
    /// </summary>
    Sent,

    /// <summary>
    /// The notification sending has failed.
    /// </summary>
    Failed,
}

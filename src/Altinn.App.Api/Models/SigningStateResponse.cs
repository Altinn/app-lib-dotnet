using Newtonsoft.Json;

namespace Altinn.App.Api.Models;

/// <summary>
/// Contains the result of a get signees request.
/// </summary>
public class SigningStateResponse
{
    /// <summary>
    /// The signees for the current task.
    /// </summary>
    public required List<SigneeState> SigneeStates { get; set; }
}

/// <summary>
/// Contains information about a signee and the current signing status.
/// </summary>
public class SigneeState
{
    /// <summary>
    /// The name of the signee.
    /// </summary>
    [JsonProperty(PropertyName = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// The organisation of the signee.
    /// </summary>
    [JsonProperty(PropertyName = "organisation")]
    public string? Organisation { get; set; }

    /// <summary>
    /// Whether the signee has signed or not.
    /// </summary>
    [JsonProperty(PropertyName = "hasSigned")]
    public required bool HasSigned { get; set; }

    /// <summary>
    /// Whether delegation of signing rights has been successful.
    /// </summary>
    [JsonProperty(PropertyName = "delegationSuccessful")]
    public bool DelegationSuccessful { get; set; }

    /// <summary>
    /// Whether the signee has been notified to sign via email or sms.
    /// </summary>
    [JsonProperty(PropertyName = "notificationSuccessful")]
    public NotificationState NotificationSuccessful { get; set; }

    /// <summary>
    /// The party id of the signee.
    /// </summary>
    [JsonProperty(PropertyName = "partyId")]
    public required int PartyId { get; set; }
    //TODO: Add necessary properties
}

/// <summary>
/// Represents the state of a notification.
/// </summary>
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

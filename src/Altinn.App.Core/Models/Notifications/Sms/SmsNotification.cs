using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models.Notifications.Sms;

/// <summary>
/// SMS notification class used for placing SMS notification orders.
/// </summary>
public sealed record SmsNotification
{
    private DateTime _requestedSendTime;

    /// <summary>
    /// The phone number to use as sender of the SMS.
    /// </summary>
    [JsonPropertyName("senderNumber")]
    public required string SenderNumber { get; init; }

    /// <summary>
    /// The phone number to use as sender of the SMS.
    /// </summary>
    [JsonPropertyName("body")]
    public required string Body { get; init; }

    /// <summary>
    /// The Requested send time for the email. 
    /// DateTime.UtcNow by default.
    /// </summary>
    [JsonPropertyName("requestedSendTime")]
    public DateTime? RequestedSendTime
    {
        get => _requestedSendTime == default ? DateTime.UtcNow : _requestedSendTime;
        init
        {
            _requestedSendTime = value switch
            {
                null => DateTime.UtcNow,
                DateTime timestamp when timestamp <= DateTime.UtcNow.AddMinutes(-1) => DateTime.UtcNow,
                DateTime timestamp => timestamp.ToUniversalTime(),
            };
        }
    }

    /// <summary>
    /// The phone number to use as sender of the SMS.
    /// </summary>
    [JsonPropertyName("sendersReference")]
    public required string SendersReference { get; init; }

    /// <summary>
    /// The recipients of the SMS. 
    /// </summary>
    [JsonPropertyName("recipients")]
    public required IReadOnlyList<SmsRecipient> Recipients { get; init; }
}

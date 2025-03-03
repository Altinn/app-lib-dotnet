namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Recipients for the notification. If not set, the notification will be sent to the recipient of the Correspondence.
/// </summary>
public sealed record CorrespondenceNotificationRecipientWrapper : MultipartCorrespondenceListItem
{
    /// <summary>
    /// The correspondance recipient which the notification should be overridden for. Organization number or national identification number.
    /// </summary>
    public required string RecipientToOverride { get; init; }

    /// <summary>
    /// List of custom recipients to override the default recipient.
    /// </summary>
    public required List<CorrespondenceNotificationRecipient> CorrespondenceNotificationRecipient { get; init; }

    internal override void Serialise(MultipartFormDataContent content, int index)
    {
        AddRequired(
            content,
            RecipientToOverride,
            $"Correspondence.NotificationRecipients[{index}].RecipientToOverride"
        );
        SerializeListItems(content, CorrespondenceNotificationRecipient);
    }
}

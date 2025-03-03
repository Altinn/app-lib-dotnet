using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder for creating <see cref="CorrespondenceNotification"/> objects with recipient overrides.
/// </summary>
public interface ICorrespondenceNotificationOverrideBuilder
{
    /// <summary>
    /// Sets the recipient to override for the correspondence notification.
    /// </summary>
    /// <param name="recipientToOverride">The recipient to override notifications for. Organization number / national identifier</param>
    public ICorrespondenceNotificationOverrideBuilder WithRecipientToOverride(string recipientToOverride);

    /// <summary>
    /// Sets the custom recipients to override the default recipient.
    /// </summary>
    /// <param name="correspondenceNotificationRecipient">The custom recipients</param>
    public ICorrespondenceNotificationOverrideBuilder WithCorrespondenceNotificationRecipients(
        List<CorrespondenceNotificationRecipient> correspondenceNotificationRecipient
    );

    /// <summary>
    /// Builds the <see cref="CorrespondenceNotificationRecipientWrapper"/> object.
    /// </summary>
    CorrespondenceNotificationRecipientWrapper Build();
}

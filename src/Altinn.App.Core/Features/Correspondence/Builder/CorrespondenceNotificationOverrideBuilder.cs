using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceNotificationRecipientWrapper"/> objects.
/// </summary>
public class CorrespondenceNotificationOverrideBuilder : ICorrespondenceNotificationOverrideBuilder
{
    private string? _recipientToOverride;
    private List<CorrespondenceNotificationRecipient>? _correspondenceNotificationRecipients;

    private CorrespondenceNotificationOverrideBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceNotificationOverrideBuilder"/> instance.
    /// </summary>
    /// <returns>The builder instance</returns>
    public static ICorrespondenceNotificationOverrideBuilder Create() =>
        new CorrespondenceNotificationOverrideBuilder();

    /// <inheritdoc/>
    public ICorrespondenceNotificationOverrideBuilder WithRecipientToOverride(string recipientToOverride)
    {
        _recipientToOverride = recipientToOverride;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationOverrideBuilder WithCorrespondenceNotificationRecipients(
        List<CorrespondenceNotificationRecipient> correspondenceNotificationRecipients
    )
    {
        _correspondenceNotificationRecipients = correspondenceNotificationRecipients;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceNotificationRecipientWrapper Build()
    {
        BuilderUtils.NotNullOrEmpty(_recipientToOverride, "Recipient to override cannot be empty");
        BuilderUtils.NotNullOrEmpty(
            _correspondenceNotificationRecipients,
            "Correspondence notification recipient cannot be empty"
        );
        return new CorrespondenceNotificationRecipientWrapper
        {
            RecipientToOverride = _recipientToOverride,
            CorrespondenceNotificationRecipients = _correspondenceNotificationRecipients,
        };
    }
}

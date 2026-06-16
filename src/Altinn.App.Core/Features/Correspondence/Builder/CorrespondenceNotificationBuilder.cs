using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceNotification"/> objects.
/// </summary>
public class CorrespondenceNotificationBuilder : ICorrespondenceNotificationBuilder
{
    private CorrespondenceNotificationTemplate? _notificationTemplate;
    private string? _emailSubject;
    private string? _emailBody;
    private string? _smsBody;
    private bool? _sendReminder;
    private string? _reminderEmailSubject;
    private string? _reminderEmailBody;
    private string? _reminderSmsBody;
    private CorrespondenceNotificationChannel? _notificationChannel;
    private CorrespondenceNotificationChannel? _reminderNotificationChannel;
    private string? _sendersReference;
    private IReadOnlyList<CorrespondenceNotificationRecipient>? _customRecipients;
    private bool _overrideRegisteredContactInformation;
    private CorrespondenceNotificationRecipient? _recipientOverride;

    [Obsolete]
    private List<CorrespondenceNotificationRecipientWrapper>? _recipientToOverrideWrapper;

    private CorrespondenceNotificationBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceNotificationBuilder"/> instance.
    /// </summary>
    public static ICorrespondenceNotificationBuilderTemplate Create() => new CorrespondenceNotificationBuilder();

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithNotificationTemplate(
        CorrespondenceNotificationTemplate notificationTemplate
    )
    {
        BuilderUtils.NotNullOrEmpty(notificationTemplate, "Notification template cannot be empty");
        _notificationTemplate = notificationTemplate;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithEmailSubject(string? emailSubject)
    {
        _emailSubject = emailSubject;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithEmailBody(string? emailBody)
    {
        _emailBody = emailBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithSmsBody(string? smsBody)
    {
        _smsBody = smsBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithSendReminder(bool? sendReminder)
    {
        _sendReminder = sendReminder;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithReminderEmailSubject(string? reminderEmailSubject)
    {
        _reminderEmailSubject = reminderEmailSubject;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithReminderEmailBody(string? reminderEmailBody)
    {
        _reminderEmailBody = reminderEmailBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithReminderSmsBody(string? reminderSmsBody)
    {
        _reminderSmsBody = reminderSmsBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithNotificationChannel(
        CorrespondenceNotificationChannel? notificationChannel
    )
    {
        _notificationChannel = notificationChannel;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithReminderNotificationChannel(
        CorrespondenceNotificationChannel? reminderNotificationChannel
    )
    {
        _reminderNotificationChannel = reminderNotificationChannel;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithSendersReference(string? sendersReference)
    {
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    [Obsolete("RequestedSendTime is no longer supported by the Correspondence API.")]
    public ICorrespondenceNotificationBuilder WithRequestedSendTime(DateTimeOffset? requestedSendTime)
    {
        // Intentional no-op: RequestedSendTime is no longer accepted by the Correspondence API.
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithCustomRecipients(
        IReadOnlyList<CorrespondenceNotificationRecipient> customRecipients
    )
    {
        _customRecipients = customRecipients;
        _overrideRegisteredContactInformation = false;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithCustomRecipientsIfConfigured(
        IReadOnlyList<CorrespondenceNotificationRecipient>? customRecipients
    )
    {
        if (customRecipients is { Count: > 0 })
        {
            return WithCustomRecipients(customRecipients);
        }

        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithRecipientOverrides(
        IReadOnlyList<CorrespondenceNotificationRecipient> recipientOverrides
    )
    {
        _customRecipients = recipientOverrides;
        _overrideRegisteredContactInformation = true;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilder WithRecipientOverridesIfConfigured(
        IReadOnlyList<CorrespondenceNotificationRecipient>? recipientOverrides
    )
    {
        if (recipientOverrides is { Count: > 0 })
        {
            return WithRecipientOverrides(recipientOverrides);
        }

        return this;
    }

    /// <inheritdoc/>
    [Obsolete("Use WithCustomRecipients instead.")]
    public ICorrespondenceNotificationBuilder WithRecipientOverride(
        ICorrespondenceNotificationOverrideBuilder recipientOverrideBuilder
    )
    {
        return WithRecipientOverride(recipientOverrideBuilder.Build());
    }

    /// <inheritdoc/>
    [Obsolete("Use WithCustomRecipients instead.")]
    public ICorrespondenceNotificationBuilder WithRecipientOverride(
        CorrespondenceNotificationRecipient recipientOverride
    )
    {
        _recipientOverride = recipientOverride;
        return this;
    }

    /// <inheritdoc/>
    [Obsolete("Use WithCustomRecipientsIfConfigured instead.")]
    public ICorrespondenceNotificationBuilder WithRecipientOverrideIfConfigured(
        CorrespondenceNotificationRecipient? recipientOverride
    )
    {
        if (recipientOverride is not null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return WithRecipientOverride(recipientOverride);
#pragma warning restore CS0618
        }

        return this;
    }

    /// <inheritdoc/>
    [Obsolete("Use WithRecipientOverride(CorrespondenceNotificationRecipient recipientOverride) instead.")]
    public ICorrespondenceNotificationBuilder WithRecipientOverride(
        CorrespondenceNotificationRecipientWrapper recipientToOverrideWrapper
    )
    {
        _recipientToOverrideWrapper ??= [];
        _recipientToOverrideWrapper.Add(recipientToOverrideWrapper);
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceNotification Build()
    {
        BuilderUtils.NotNullOrEmpty(_notificationTemplate);

        return new CorrespondenceNotification
        {
            NotificationTemplate = _notificationTemplate.Value,
            EmailSubject = _emailSubject,
            EmailBody = _emailBody,
            SmsBody = _smsBody,
            SendReminder = _sendReminder,
            ReminderEmailSubject = _reminderEmailSubject,
            ReminderEmailBody = _reminderEmailBody,
            ReminderSmsBody = _reminderSmsBody,
            NotificationChannel = _notificationChannel,
            ReminderNotificationChannel = _reminderNotificationChannel,
            SendersReference = _sendersReference,
            CustomRecipients = _customRecipients,
            OverrideRegisteredContactInformation = _overrideRegisteredContactInformation,
#pragma warning disable CS0618 // Type or member is obsolete - retained for backwards compatibility
            CustomRecipient = _recipientOverride,
#pragma warning restore CS0618
        };
    }
}

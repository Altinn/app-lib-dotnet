using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceNotification"/> objects
/// </summary>
public class CorrespondenceNotificationBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceNotificationBuilderNeedsTemplate,
        ICorrespondenceNotificationBuilderCanBuild
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
    private DateTimeOffset? _requestedSendTime;

    private CorrespondenceNotificationBuilder() { }

    /// <summary>
    /// Creates a new <see cref="CorrespondenceNotificationBuilder"/> instance
    /// </summary>
    /// <returns></returns>
    public static ICorrespondenceNotificationBuilderNeedsTemplate Create() => new CorrespondenceNotificationBuilder();

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithNotificationTemplate(
        CorrespondenceNotificationTemplate notificationTemplate
    )
    {
        _notificationTemplate = notificationTemplate;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithEmailSubject(string? emailSubject)
    {
        _emailSubject = emailSubject;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithEmailBody(string? emailBody)
    {
        _emailBody = emailBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithSmsBody(string? smsBody)
    {
        _smsBody = smsBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithSendReminder(bool? sendReminder)
    {
        _sendReminder = sendReminder;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithReminderEmailSubject(string? reminderEmailSubject)
    {
        _reminderEmailSubject = reminderEmailSubject;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithReminderEmailBody(string? reminderEmailBody)
    {
        _reminderEmailBody = reminderEmailBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithReminderSmsBody(string? reminderSmsBody)
    {
        _reminderSmsBody = reminderSmsBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithNotificationChannel(
        CorrespondenceNotificationChannel? notificationChannel
    )
    {
        _notificationChannel = notificationChannel;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithReminderNotificationChannel(
        CorrespondenceNotificationChannel? reminderNotificationChannel
    )
    {
        _reminderNotificationChannel = reminderNotificationChannel;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithSendersReference(string? sendersReference)
    {
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderCanBuild WithRequestedSendTime(DateTimeOffset? requestedSendTime)
    {
        _requestedSendTime = requestedSendTime;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceNotification Build()
    {
        NotNullOrEmpty(_notificationTemplate, "Notification template is required");

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
            RequestedSendTime = _requestedSendTime
        };
    }
}

using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Builder factory for creating <see cref="CorrespondenceNotification"/> objects
/// </summary>
public class CorrespondenceNotificationBuilder
    : CorrespondenceBuilderBase,
        ICorrespondenceNotificationBuilderTemplate,
        ICorrespondenceNotificationBuilderBuild
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
    public static ICorrespondenceNotificationBuilderTemplate Create() => new CorrespondenceNotificationBuilder();

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithNotificationTemplate(
        CorrespondenceNotificationTemplate notificationTemplate
    )
    {
        _notificationTemplate = notificationTemplate;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithEmailSubject(string? emailSubject)
    {
        _emailSubject = emailSubject;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithEmailBody(string? emailBody)
    {
        _emailBody = emailBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithSmsBody(string? smsBody)
    {
        _smsBody = smsBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithSendReminder(bool? sendReminder)
    {
        _sendReminder = sendReminder;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithReminderEmailSubject(string? reminderEmailSubject)
    {
        _reminderEmailSubject = reminderEmailSubject;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithReminderEmailBody(string? reminderEmailBody)
    {
        _reminderEmailBody = reminderEmailBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithReminderSmsBody(string? reminderSmsBody)
    {
        _reminderSmsBody = reminderSmsBody;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithNotificationChannel(
        CorrespondenceNotificationChannel? notificationChannel
    )
    {
        _notificationChannel = notificationChannel;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithReminderNotificationChannel(
        CorrespondenceNotificationChannel? reminderNotificationChannel
    )
    {
        _reminderNotificationChannel = reminderNotificationChannel;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithSendersReference(string? sendersReference)
    {
        _sendersReference = sendersReference;
        return this;
    }

    /// <inheritdoc/>
    public ICorrespondenceNotificationBuilderBuild WithRequestedSendTime(DateTimeOffset? requestedSendTime)
    {
        _requestedSendTime = requestedSendTime;
        return this;
    }

    /// <inheritdoc/>
    public CorrespondenceNotification Build()
    {
        NotNull(_notificationTemplate, "Notification template is required");

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

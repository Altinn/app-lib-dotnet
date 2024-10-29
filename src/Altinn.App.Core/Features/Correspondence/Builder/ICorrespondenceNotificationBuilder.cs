using Altinn.App.Core.Features.Correspondence.Models;

namespace Altinn.App.Core.Features.Correspondence.Builder;

/// <summary>
/// Indicates that the <see cref="CorrespondenceNotificationBuilder"/> instance is on the <see cref="CorrespondenceNotification.NotificationTemplate"/> step
/// </summary>
public interface ICorrespondenceNotificationBuilderTemplate
{
    /// <summary>
    /// Sets the notification template for the correspondence notification
    /// </summary>
    /// <param name="notificationTemplate">The notification template</param>
    ICorrespondenceNotificationBuilderBuild WithNotificationTemplate(
        CorrespondenceNotificationTemplate notificationTemplate
    );
}

/// <summary>
/// Indicates that the <see cref="CorrespondenceNotificationBuilder"/> instance has completed all required steps and can proceed to <see cref="CorrespondenceNotificationBuilder.Build"/>
/// </summary>
public interface ICorrespondenceNotificationBuilderBuild
{
    /// <summary>
    /// Sets the email subject for the correspondence notification
    /// </summary>
    /// <remarks>
    /// Depending on the <see cref="CorrespondenceNotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// <param name="emailSubject">The email subject</param>
    ICorrespondenceNotificationBuilderBuild WithEmailSubject(string? emailSubject);

    /// <summary>
    /// Sets the email body for the correspondence notification
    /// </summary>
    /// <remarks>
    /// Depending on the <see cref="CorrespondenceNotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// <param name="emailBody">The email content (body)</param>
    ICorrespondenceNotificationBuilderBuild WithEmailBody(string? emailBody);

    /// <summary>
    /// Sets the SMS body for the correspondence notification
    /// </summary>
    /// <remarks>
    /// Depending on the <see cref="CorrespondenceNotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// <param name="smsBody">The SMS content (body)</param>
    ICorrespondenceNotificationBuilderBuild WithSmsBody(string? smsBody);

    /// <summary>
    /// Sets whether a reminder should be sent for the correspondence notification, if not actioned within an appropriate time frame
    /// </summary>
    /// <param name="sendReminder">`true` if yes, `false` if no</param>
    ICorrespondenceNotificationBuilderBuild WithSendReminder(bool? sendReminder);

    /// <summary>
    /// Sets the email subject for the reminder notification
    /// </summary>
    /// <remarks>
    /// Depending on the <see cref="CorrespondenceNotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// <param name="reminderEmailSubject">The reminder email subject</param>
    ICorrespondenceNotificationBuilderBuild WithReminderEmailSubject(string? reminderEmailSubject);

    /// <summary>
    /// Sets the email body for the reminder notification
    /// </summary>
    /// <remarks>
    /// Depending on the <see cref="CorrespondenceNotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// <param name="reminderEmailBody">The reminder email content (body)</param>
    ICorrespondenceNotificationBuilderBuild WithReminderEmailBody(string? reminderEmailBody);

    /// <summary>
    /// Sets the SMS body for the reminder notification
    /// </summary>
    /// <remarks>
    /// Depending on the <see cref="CorrespondenceNotificationTemplate"/> in use, this value may be padded according to the template logic
    /// </remarks>
    /// <param name="reminderSmsBody">The reminder SMS content (body)</param>
    ICorrespondenceNotificationBuilderBuild WithReminderSmsBody(string? reminderSmsBody);

    /// <summary>
    /// Sets the notification channel for the correspondence notification
    /// </summary>
    /// <param name="notificationChannel">The notification channel to use</param>
    ICorrespondenceNotificationBuilderBuild WithNotificationChannel(
        CorrespondenceNotificationChannel? notificationChannel
    );

    /// <summary>
    /// Sets the notification channel for the reminder notification
    /// </summary>
    /// <param name="reminderNotificationChannel">The notification channel to use</param>
    ICorrespondenceNotificationBuilderBuild WithReminderNotificationChannel(
        CorrespondenceNotificationChannel? reminderNotificationChannel
    );

    /// <summary>
    /// Sets the senders reference for the correspondence notification
    /// </summary>
    /// <param name="sendersReference">The senders reference value</param>
    /// <returns></returns>
    ICorrespondenceNotificationBuilderBuild WithSendersReference(string? sendersReference);

    /// <summary>
    /// Sets the requested send time for the correspondence notification
    /// </summary>
    /// <param name="requestedSendTime">The requested send time</param>
    ICorrespondenceNotificationBuilderBuild WithRequestedSendTime(DateTimeOffset? requestedSendTime);

    /// <summary>
    /// Builds the <see cref="CorrespondenceNotification"/> instance
    /// </summary>
    CorrespondenceNotification Build();
}

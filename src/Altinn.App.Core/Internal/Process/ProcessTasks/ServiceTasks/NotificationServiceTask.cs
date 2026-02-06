using System.Configuration;
using Altinn.App.Core.Features.Notifications;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.Notifications;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

internal sealed class NotificationServiceTask : IServiceTask
{
    private readonly IProcessReader _processReader;
    private readonly INotificationService _notificationService;
    private readonly INotificationReader _notificationReader;

    public string Type => "notify";

    public NotificationServiceTask(
        IProcessReader processReader,
        INotificationService notificationService,
        INotificationReader notificationReader
    )
    {
        _processReader = processReader;
        _notificationService = notificationService;
        _notificationReader = notificationReader;
    }

    public async Task<ServiceTaskResult> Execute(ServiceTaskContext context)
    {
        string taskId = context.InstanceDataMutator.Instance.Process.CurrentTask.ElementId;

        ValidAltinnNotificationConfiguration notificationConfig = GetValidNotificationConfig(taskId);

        NotificationsWrapper notificationsWrapper = new();
        if (string.IsNullOrWhiteSpace(notificationConfig.NotificationProviderId) is false)
        {
            try
            {
                notificationsWrapper = _notificationReader.GetProvidedNotifications(
                    notificationConfig.NotificationProviderId,
                    context.CancellationToken
                );
            }
            catch (ConfigurationException ex)
            {
                // TODO: log. For now, rethrowing to explicitly show the exception that is expected for invalid configuration.
                throw ex;
            }
        }

        var smsToProcess = notificationsWrapper.SmsNotification;

        _ = await _notificationService.NotifyInstanceOwner(
            context.InstanceDataMutator.Instance,
            new EmailOverride(),
            new SmsOverride(),
            context.CancellationToken
        );

        return ServiceTaskResult.Success();
    }

    private ValidAltinnNotificationConfiguration GetValidNotificationConfig(string taskId)
    {
        AltinnTaskExtension? altinnTaskExtension = _processReader.GetAltinnTaskExtension(taskId);
        AltinnNotificationConfiguration? notificationConfig = altinnTaskExtension?.NotificationConfiguration;

        if (notificationConfig == null)
        {
            // No notification configuration specified, return an empty configuration.
            return new ValidAltinnNotificationConfiguration();
        }

        return notificationConfig.Validate();
    }
}

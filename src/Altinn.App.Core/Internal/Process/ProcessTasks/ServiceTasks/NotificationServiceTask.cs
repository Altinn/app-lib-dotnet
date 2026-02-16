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

        if (string.IsNullOrWhiteSpace(notificationConfig.NotificationProviderId) is false)
        {
            await HandleInterfaceProvidedNotifications(
                notificationConfig.NotificationProviderId,
                context.CancellationToken
            );
        }

        if (notificationConfig.SmsOverride is not null || notificationConfig.EmailOverride is not null)
        {
            await HandleProcessConfigurationProvidedNotifications(context, notificationConfig);
        }

        return ServiceTaskResult.Success();
    }

    private async Task HandleProcessConfigurationProvidedNotifications(
        ServiceTaskContext context,
        ValidAltinnNotificationConfiguration notificationConfig
    )
    {
        List<NotificationReference> references = await _notificationService.NotifyInstanceOwner(
            context.InstanceDataMutator.Instance,
            notificationConfig.EmailOverride ?? new EmailOverride(),
            notificationConfig.SmsOverride ?? new SmsOverride(),
            context.CancellationToken
        );
    }

    private async Task HandleInterfaceProvidedNotifications(string notificationProviderId, CancellationToken ct)
    {
        try
        {
            NotificationsWrapper notificationsWrapper = _notificationReader.GetProvidedNotifications(
                notificationProviderId,
                ct
            );

            List<NotificationReference> references = await _notificationService.ProcessNotificationOrders(
                notificationsWrapper.EmailNotifications ?? [],
                notificationsWrapper.SmsNotifications ?? [],
                ct
            );
        }
        catch (ConfigurationException ex)
        {
            // TODO: log. For now, rethrowing to explicitly show the exception that is expected for invalid configuration.
            throw ex;
        }
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

using System.Configuration;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Notifications;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks;

internal sealed class NotificationServiceTask : IServiceTask
{
    private readonly IProcessReader _processReader;
    private readonly INotificationService _notificationService;
    private readonly INotificationReader _notificationReader;
    private readonly IAuthenticationContext _authenticationContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string Type => "notify";

    public NotificationServiceTask(
        IProcessReader processReader,
        INotificationService notificationService,
        INotificationReader notificationReader,
        IAuthenticationContext authenticationContext,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _processReader = processReader;
        _notificationService = notificationService;
        _notificationReader = notificationReader;
        _authenticationContext = authenticationContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceTaskResult> Execute(ServiceTaskContext context)
    {
        CancellationToken ct = context.CancellationToken;
        string taskId = context.InstanceDataMutator.Instance.Process.CurrentTask.ElementId;

        ValidAltinnNotificationConfiguration notificationConfig = GetValidNotificationConfig(taskId);

        string language = await GetLanguageFromContext();

        if (string.IsNullOrWhiteSpace(notificationConfig.NotificationProviderId) is false)
        {
            await HandleInterfaceProvidedNotifications(notificationConfig.NotificationProviderId, language, ct);
        }

        if (notificationConfig.SmsOverride is not null || notificationConfig.EmailOverride is not null)
        {
            await HandleProcessConfigurationProvidedNotifications(context, notificationConfig, language, ct);
        }

        return ServiceTaskResult.Success();
    }

    private async Task<string> GetLanguageFromContext()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        var queries = httpContext?.Request.Query;
        var auth = _authenticationContext.Current;

        var language = GetOverriddenLanguage(queries) ?? await auth.GetLanguage();
        return language;
    }

    private static string? GetOverriddenLanguage(IQueryCollection? queries)
    {
        if (queries is null)
        {
            return null;
        }

        if (
            queries.TryGetValue("language", out StringValues queryLanguage)
            || queries.TryGetValue("lang", out queryLanguage)
        )
        {
            return queryLanguage.ToString();
        }

        return null;
    }

    private async Task HandleProcessConfigurationProvidedNotifications(
        ServiceTaskContext context,
        ValidAltinnNotificationConfiguration notificationConfig,
        string language,
        CancellationToken ct
    )
    {
        List<NotificationReference> references = await _notificationService.NotifyInstanceOwner(
            language,
            context.InstanceDataMutator.Instance,
            notificationConfig.EmailOverride ?? new EmailOverride(),
            notificationConfig.SmsOverride ?? new SmsOverride(),
            ct
        );
    }

    private async Task HandleInterfaceProvidedNotifications(
        string notificationProviderId,
        string language,
        CancellationToken ct
    )
    {
        try
        {
            NotificationsWrapper nw = _notificationReader.GetProvidedNotifications(notificationProviderId, ct);

            List<NotificationReference> references = await _notificationService.ProcessNotificationOrders(
                language,
                nw.EmailNotifications ?? [],
                nw.SmsNotifications ?? [],
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

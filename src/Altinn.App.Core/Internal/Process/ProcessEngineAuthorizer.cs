using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process;

/// <summary>
/// Authorizer for the process engine.
/// </summary>
internal sealed class ProcessEngineAuthorizer : IProcessEngineAuthorizer
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProcessEngineAuthorizer> _logger;

    public ProcessEngineAuthorizer(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessEngineAuthorizer> logger
    )
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Use this to determine if the user is allowed to perform process next for the current task.
    /// </summary>
    public async Task<bool> AuthorizeProcessNext(Instance instance, string? action = null)
    {
        if (instance.Process.CurrentTask is null)
        {
            _logger.LogError(
                $"Instance {instance.Id} has no current task. The process must be started before process next can be authorized."
            );
            return false;
        }

        string currentTaskId = instance.Process.CurrentTask.ElementId;
        string altinnTaskType = instance.Process.CurrentTask.AltinnTaskType;

        // When an action is provided we only allow process next if that action is allowed.
        if (action is not null)
        {
            bool isPerformedActionAuthorized = await _authorizationService.AuthorizeAction(
                new AppIdentifier(instance.AppId),
                new InstanceIdentifier(instance),
                _httpContext.User,
                action,
                currentTaskId
            );

            _logger.LogInformation(
                $"Process next was performed with action: {LogSanitizer.Sanitize(action)}. Authorization result: {isPerformedActionAuthorized}."
            );

            return isPerformedActionAuthorized;
        }

        // When no action is provided we check if the user is authorized for at least one of the actions that allow process next for the current task type.
        string[] actionsThatAllowProcessNextForTaskType = GetActionsThatAllowProcessNextForTaskType(altinnTaskType);

        var isAnyActionAuthorized = false;
        foreach (string actionToAuthorize in actionsThatAllowProcessNextForTaskType)
        {
            bool isActionAuthorized = await _authorizationService.AuthorizeAction(
                new AppIdentifier(instance.AppId),
                new InstanceIdentifier(instance),
                _httpContext.User,
                actionToAuthorize,
                currentTaskId
            );

            if (isActionAuthorized)
            {
                isAnyActionAuthorized = true;
                break;
            }
        }

        _logger.LogInformation(
            $"Process next performed without an action. Authorizing based on task type {altinnTaskType}, which means using action(s) [{string.Join(",", actionsThatAllowProcessNextForTaskType)}]. Authorization result: {isAnyActionAuthorized}."
        );

        return isAnyActionAuthorized;
    }

    private HttpContext _httpContext =>
        _httpContextAccessor.HttpContext ?? throw new AuthenticationContextException("No HTTP context available");

    /// <summary>
    /// Get all actions that allow process next for the given task type. Meant to be used to authorize the process next when no action is provided.
    /// </summary>
    /// <remarks>To allow process next for a custom action, user needs to have access to an action with the same name as the task type, or alternatively 'write', in the policy.</remarks>
    private static string[] GetActionsThatAllowProcessNextForTaskType(string taskType)
    {
        return taskType switch
        {
            "data" or "feedback" => ["write"],
            "payment" => ["pay", "write"],
            "confirmation" => ["confirm"],
            "signing" => ["sign", "write"],
            _ => [taskType, "write"],
        };
    }
}

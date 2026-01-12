using System.Security.Claims;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.Auth;

internal sealed class AuthorizationCache : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, bool> _cache = new();
    private readonly AppIdentifier _appIdentifier;
    private readonly IAuthorizationClient _authorizationClient;
    private readonly IProcessReader _processReader;

    public AuthorizationCache(
        AppIdentifier appIdentifier,
        IAuthorizationClient authorizationClient,
        IProcessReader processReader
    )
    {
        _appIdentifier = appIdentifier;
        _authorizationClient = authorizationClient;
        _processReader = processReader;
    }

    private static string FormatKey(InstanceIdentifier identifier, string action, string? taskId, string? endEvent)
    {
        return $"{identifier.InstanceGuid}|{taskId ?? "null"}|{action}|{endEvent ?? "null"}";
    }

    private List<string> GetActionsToAuthorizeForTask(string? taskId)
    {
        var actions = new List<string>() { "read", "write", "delete" };
        if (taskId is not null)
        {
            var taskExtensions = _processReader.GetAltinnTaskExtension(taskId);
            if (taskExtensions?.AltinnActions is { Count: > 0 } bpmnActions)
            {
                actions.AddRange(bpmnActions.Select(a => a.Value));
            }
        }
        return actions;
    }

    public async Task<bool> AuthorizeAction(
        ClaimsPrincipal user,
        InstanceIdentifier identifier,
        string action,
        string? taskId,
        string? endEvent = null
    )
    {
        string key = FormatKey(identifier, action, taskId, endEvent);

        await _semaphore.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out bool isAuthorizedCached))
            {
                return isAuthorizedCached;
            }
            var actionsToAuthorize = GetActionsToAuthorizeForTask(taskId);
            if (actionsToAuthorize.Contains(action))
            {
                // Authorize all actions for the task in one go
                var results = await _authorizationClient.AuthorizeActions(
                    _appIdentifier,
                    identifier,
                    user,
                    actionsToAuthorize,
                    taskId,
                    endEvent
                );
                foreach (var (authAction, isAuth) in results)
                {
                    string actionKey = FormatKey(identifier, authAction, taskId, endEvent);
                    _cache[actionKey] = isAuth;
                }
                return _cache[key];
            }

            // Authorize single action if it is not part of the task's actions
            bool isAuthorized = await _authorizationClient.AuthorizeAction(
                _appIdentifier,
                identifier,
                user,
                action,
                taskId
            );
            _cache[key] = isAuthorized;
            return isAuthorized;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}

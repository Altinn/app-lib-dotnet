using System.Collections.Frozen;
using System.Diagnostics;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.App;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Altinn.App.Api.Infrastructure.Filters;

internal static class ScopeAuthorizationFilterDI
{
    internal static IServiceCollection AddScopeAuthorizationFilter(this IServiceCollection services)
    {
        services.AddSingleton<CustomActionDescriptorProvider>();
        services.AddSingleton<IActionDescriptorProvider>(sp => sp.GetRequiredService<CustomActionDescriptorProvider>());
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add<ScopeAuthorizationFilter>();
        });
        return services;
    }
}

internal sealed class ScopeAuthorizationFilter(
    ILogger<ScopeAuthorizationFilter> logger,
    CustomActionDescriptorProvider provider
) : IAsyncActionFilter
{
    private readonly ILogger<ScopeAuthorizationFilter> _logger = logger;
    private readonly CustomActionDescriptorProvider _provider = provider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        _provider.TryInitialize();

        var key = CustomActionDescriptorProvider.RequiredScopesKey;
        var user = context.HttpContext.User;
        var isAuth = user.Identity?.IsAuthenticated ?? false;
        if (context.ActionDescriptor.Properties.TryGetValue(key, out var scopesSetObj) && isAuth)
        {
            var scopesSet = scopesSetObj as FrozenSet<string>;
            Debug.Assert(scopesSet is not null, "Scopes should be a set of strings");
            var scopeClaim = user.FindFirst("urn:altinn:scope") ?? user.FindFirst("scope");
            var scopes = new Scopes(scopeClaim?.Value);
            if (!HasAnyScope(in scopes, scopesSet))
            {
                _logger.LogWarning(
                    "User does not have required scope for action '{Action}'",
                    context.ActionDescriptor.DisplayName
                );

                context.Result = new ForbidResult();
                return;
            }
        }

        await next();
    }

    private static bool HasAnyScope(in Scopes scopes, FrozenSet<string> scopesSet)
    {
        Debug.Assert(scopesSet.Count > 0);
        foreach (var scope in scopes)
        {
            if (scopesSet.Contains(scope.ToString()))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class CustomActionDescriptorProvider(IAppMetadata appMetadata) : IActionDescriptorProvider
{
    public int Order => 0;

    private readonly IAppMetadata _appMetadata = appMetadata;

    private List<ControllerActionDescriptor>? _actionsToAuthorize;
    private List<ControllerActionDescriptor>? _actionsNotAuthorized;

    public IReadOnlyList<ControllerActionDescriptor>? ActionsToAuthorize => _actionsToAuthorize;

    public IReadOnlyList<ControllerActionDescriptor>? ActionsNotAuthorized => _actionsNotAuthorized;

    private static readonly FrozenSet<string> _readHttpMethods = new HashSet<string>
    {
        "GET",
        "HEAD",
        "OPTIONS",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private enum ScopeType
    {
        Read,
        Write,
    }

    private static readonly FrozenDictionary<string, ScopeType> _manuallyIncludeActions = new Dictionary<
        string,
        ScopeType
    >
    {
        ["Altinn.App.Api.Controllers.InstancesController.PostSimplified (Altinn.App.Api)"] = ScopeType.Write,
        ["Altinn.App.Api.Controllers.StatelessDataController.Get (Altinn.App.Api)"] = ScopeType.Read,
        ["Altinn.App.Api.Controllers.StatelessDataController.Post (Altinn.App.Api)"] = ScopeType.Read,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    internal static readonly object RequiredScopesKey = new();
    private readonly object _lock = new();

    private ActionDescriptorProviderContext? _context;

    // TODO: do this in the normal hook
    internal void TryInitialize()
    {
        if (_actionsToAuthorize is not null)
            return;

        lock (_lock)
        {
            if (_actionsToAuthorize is not null)
                return;

            _actionsToAuthorize = new List<ControllerActionDescriptor>();
            _actionsNotAuthorized = new List<ControllerActionDescriptor>();

            var context =
                _context ?? throw new InvalidOperationException("Context not set. Call Process with a valid context");
            var appMetadata = _appMetadata.GetApplicationMetadata().GetAwaiter().GetResult();

            var dedupe = new Dictionary<string, IEnumerable<string>>();

            if (
                !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.Read)
                || !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.Write)
            )
            {
                foreach (var action in context.Results.OfType<ControllerActionDescriptor>())
                {
                    var httpMethodAttr = (HttpMethodAttribute?)
                        action.EndpointMetadata.SingleOrDefault(m => m is HttpMethodAttribute);
                    var httpMethods = httpMethodAttr?.HttpMethods ?? ["GET", "POST"];
                    if (dedupe.TryGetValue(action.DisplayName ?? "", out var existingHttpMethods))
                    {
                        if (!existingHttpMethods.SequenceEqual(httpMethods))
                        {
                            throw new InvalidOperationException(
                                $"Duplicate action '{action.DisplayName}' with different HTTP methods detected"
                            );
                        }
                        continue;
                    }

                    dedupe.Add(action.DisplayName ?? "", httpMethods);

                    if (_manuallyIncludeActions.TryGetValue(action.DisplayName ?? "", out var scopeType))
                    {
                        var scopesSet = CreateScopesSet(scopeType, appMetadata.ApiScopes);
                        if (scopesSet is not null)
                        {
                            _actionsToAuthorize.Add(action);
                            action.Properties[RequiredScopesKey] = scopesSet;
                        }
                        else
                        {
                            _actionsNotAuthorized.Add(action);
                        }
                        continue;
                    }

                    scopeType = httpMethods.All(m => _readHttpMethods.Contains(m)) ? ScopeType.Read : ScopeType.Write;

                    var hasAllowAnonymousAttr = action.EndpointMetadata.Any(m => m is AllowAnonymousAttribute);

                    if (
                        !hasAllowAnonymousAttr
                        && action.Parameters.Any(p =>
                            p.Name == "instanceGuid" || p.Name == "instanceId" || p.Name == "instanceOwnerPartyId"
                        )
                    )
                    {
                        var scopesSet = CreateScopesSet(scopeType, appMetadata.ApiScopes);
                        if (scopesSet is not null)
                        {
                            _actionsToAuthorize.Add(action);
                            action.Properties[RequiredScopesKey] = scopesSet;
                        }
                        else
                        {
                            _actionsNotAuthorized.Add(action);
                        }
                    }
                    else
                    {
                        _actionsNotAuthorized.Add(action);
                    }
                }
            }
        }
    }

    private static FrozenSet<string>? CreateScopesSet(ScopeType type, ApiScopes? apiScopes)
    {
        var configuredScope = type == ScopeType.Read ? apiScopes?.Read : apiScopes?.Write;
        if (string.IsNullOrWhiteSpace(configuredScope))
            return null;

        var serviceOwnerScope =
            type == ScopeType.Read ? "altinn:serviceowner/instances.read" : "altinn:serviceowner/instances.write";
        return new HashSet<string>([configuredScope, "altinn:portal/enduser", serviceOwnerScope]).ToFrozenSet(
            StringComparer.Ordinal
        );
    }

    public void OnProvidersExecuted(ActionDescriptorProviderContext context) => _context = context;

    public void OnProvidersExecuting(ActionDescriptorProviderContext context) { }
}

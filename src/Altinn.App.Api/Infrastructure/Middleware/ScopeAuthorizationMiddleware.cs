using System.Collections.Frozen;
using System.Diagnostics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Cache;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Infrastructure.Middleware;

internal static class ScopeAuthorizationDI
{
    /// <summary>
    /// Adds scope authorization services and middleware to the service collection.
    /// This works with both MVC controllers and minimal APIs.
    /// </summary>
    internal static IServiceCollection AddScopeAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<ScopeAuthorizationService>();
        services.AddHostedService<ScopeAuthorizationService>(sp => sp.GetRequiredService<ScopeAuthorizationService>());
        return services;
    }

    public static TBuilder AddScopesRequirementMetadata<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new ScopeRequirementMetadata());
        });

        return builder;
    }
}

internal sealed class ScopeRequirementMetadata()
{
    public FrozenSet<string>? RequiredScopes { get; internal set; }
}

internal sealed class ScopeAuthorizationMiddleware(RequestDelegate next, ILogger<ScopeAuthorizationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ScopeAuthorizationMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var isAuth = user.Identity?.IsAuthenticated ?? false;

        if (isAuth)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint is null)
            {
                throw new InvalidOperationException(
                    "Endpoint is null. Ensure the middleware is registered after routing."
                );
            }

            // Get scopes from endpoint metadata
            var scopeMetadata = endpoint.Metadata.GetMetadata<ScopeRequirementMetadata>();
            if (scopeMetadata?.RequiredScopes is { } scopesSet)
            {
                var scopeClaim = user.FindFirst("urn:altinn:scope") ?? user.FindFirst("scope");
                var scopes = new Scopes(scopeClaim?.Value);
                if (!HasAnyScope(in scopes, scopesSet))
                {
                    _logger.LogWarning(
                        "User does not have required scope for endpoint '{Endpoint}'",
                        ScopeAuthorizationService.GetEndpointDisplayName(endpoint)
                    );

                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }
            }
        }

        await _next(context);
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

internal sealed class ScopeAuthorizationService(
    IAppConfigurationCache _appConfigurationCache,
    EndpointDataSource _endpointDataSource,
    IOptions<GeneralSettings> _generalSettings,
    ILogger<ScopeAuthorizationService> _logger
) : IHostedService
{
    private readonly List<string> _endpointsToAuthorize = [];
    private readonly List<string> _endpointsNotAuthorized = [];
    private bool _initialized;

    public IReadOnlyList<string>? EndpointsToAuthorize => _endpointsToAuthorize;
    public IReadOnlyList<string>? EndpointsNotAuthorized => _endpointsNotAuthorized;

    private static readonly FrozenSet<string> _readHttpMethods = new[] { "GET", "HEAD", "OPTIONS" }.ToFrozenSet(
        StringComparer.OrdinalIgnoreCase
    );

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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void Initialize()
    {
        if (_generalSettings.Value.IsTest)
            _initialized = true; // Skip initialization during WAF tests

        if (_initialized)
            return;

        try
        {
            var appMetadata = _appConfigurationCache.ApplicationMetadata;
            var dedupe = new Dictionary<string, IEnumerable<string>>();

            if (
                !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.Read)
                || !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.Write)
            )
            {
                ProcessEndpoints(appMetadata, dedupe);
            }

            _initialized = true;
            _logger.LogInformation(
                "Scope authorization initialized. Endpoints to authorize: {ToAuthorize}, Not authorized: {NotAuthorized}",
                _endpointsToAuthorize.Count,
                _endpointsNotAuthorized.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize scope authorization service");
            throw;
        }
    }

    private void ProcessEndpoints(ApplicationMetadata appMetadata, Dictionary<string, IEnumerable<string>> dedupe)
    {
        foreach (var endpoint in _endpointDataSource.Endpoints)
        {
            if (endpoint is not RouteEndpoint routeEndpoint)
                continue;

            var displayName = GetEndpointDisplayName(routeEndpoint);
            var httpMethods = GetEndpointHttpMethods(routeEndpoint);

            // Skip duplicates (same logic as original)
            if (dedupe.TryGetValue(displayName, out var existingHttpMethods))
            {
                if (!existingHttpMethods.SequenceEqual(httpMethods))
                {
                    throw new InvalidOperationException(
                        $"Duplicate endpoint '{displayName}' with different HTTP methods detected"
                    );
                }
                continue;
            }

            dedupe.Add(displayName, httpMethods);

            // Check for manual inclusion
            if (_manuallyIncludeActions.TryGetValue(displayName, out var scopeType))
            {
                ProcessManuallyIncludedEndpoint(routeEndpoint, scopeType, appMetadata.ApiScopes, displayName);
                continue;
            }

            // Determine scope type based on HTTP methods
            scopeType = httpMethods.All(m => _readHttpMethods.Contains(m)) ? ScopeType.Read : ScopeType.Write;

            // Check if endpoint should be authorized
            if (ShouldAuthorizeEndpoint(routeEndpoint))
            {
                var scopesSet = CreateScopesSet(scopeType, appMetadata.ApiScopes);
                if (scopesSet is not null)
                {
                    _endpointsToAuthorize.Add(displayName);

                    var metadata = routeEndpoint.Metadata.GetMetadata<ScopeRequirementMetadata>();
                    if (metadata is null)
                        throw new InvalidOperationException(
                            $"Endpoint '{displayName}' does not have ScopeRequirementMetadata"
                        );
                    metadata.RequiredScopes = scopesSet;
                }
                else
                {
                    _endpointsNotAuthorized.Add(displayName);
                }
            }
            else
            {
                _endpointsNotAuthorized.Add(displayName);
            }
        }
    }

    private void ProcessManuallyIncludedEndpoint(
        RouteEndpoint endpoint,
        ScopeType scopeType,
        ApiScopes? apiScopes,
        string displayName
    )
    {
        var scopesSet = CreateScopesSet(scopeType, apiScopes);
        if (scopesSet is not null)
        {
            _endpointsToAuthorize.Add(displayName);

            var metadata = endpoint.Metadata.GetMetadata<ScopeRequirementMetadata>();
            if (metadata is null)
                throw new InvalidOperationException($"Endpoint '{displayName}' does not have ScopeRequirementMetadata");
            metadata.RequiredScopes = scopesSet;
        }
        else
        {
            _endpointsNotAuthorized.Add(displayName);
        }
    }

    internal static string GetEndpointDisplayName(Endpoint endpoint)
    {
        // For MVC controllers, use the existing display name
        if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is { } actionDescriptor)
        {
            return actionDescriptor.DisplayName ?? "";
        }

        // For minimal APIs, create a display name based on route pattern and method
        var routePattern = endpoint is RouteEndpoint route ? route.RoutePattern.RawText ?? "" : "";
        var httpMethods = GetEndpointHttpMethods(endpoint);
        return $"{string.Join(",", httpMethods)} {routePattern}";
    }

    internal static IEnumerable<string> GetEndpointHttpMethods(Endpoint endpoint)
    {
        // Check for HTTP method metadata
        var httpMethodMetadata = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>();
        IReadOnlyList<string> httpMethods = ["GET", "POST"];
        if (httpMethodMetadata?.HttpMethods != null && httpMethodMetadata.HttpMethods.Any())
            httpMethods = httpMethodMetadata.HttpMethods;

        // For MVC controllers, check the action descriptor
        if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is { } actionDescriptor)
        {
            var httpMethodAttr = actionDescriptor.EndpointMetadata.OfType<IHttpMethodMetadata>().FirstOrDefault();
            if (httpMethodAttr?.HttpMethods != null && httpMethodAttr.HttpMethods.Any())
                httpMethods = httpMethodAttr.HttpMethods;
        }

        return httpMethods.Order().ToArray();
    }

    private static bool ShouldAuthorizeEndpoint(RouteEndpoint endpoint)
    {
        // Skip endpoints with AllowAnonymous
        if (endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null)
            return false;

        // Check route parameters for instance-related endpoints
        var routePattern = endpoint.RoutePattern;
        var hasInstanceParameters = routePattern.Parameters.Any(p =>
            string.Equals(p.Name, "instanceGuid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Name, "instanceId", StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.Name, "instanceOwnerPartyId", StringComparison.OrdinalIgnoreCase)
        );

        return hasInstanceParameters;
    }

    private static FrozenSet<string>? CreateScopesSet(ScopeType type, ApiScopes? apiScopes)
    {
        var configuredScope = type == ScopeType.Read ? apiScopes?.Read : apiScopes?.Write;
        if (string.IsNullOrWhiteSpace(configuredScope))
            return null;

        var serviceOwnerScope =
            type == ScopeType.Read ? "altinn:serviceowner/instances.read" : "altinn:serviceowner/instances.write";
        return new[] { configuredScope, "altinn:portal/enduser", serviceOwnerScope }.ToFrozenSet(
            StringComparer.Ordinal
        );
    }
}

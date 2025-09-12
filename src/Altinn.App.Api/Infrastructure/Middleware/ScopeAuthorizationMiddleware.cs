using System.Collections.Frozen;
using System.Diagnostics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Cache;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public string? ErrorMessageTextResourceKeyUser { get; internal set; }
    public string? ErrorMessageTextResourceKeyServiceOwner { get; internal set; }
    public FrozenSet<string>? RequiredScopesUsers { get; internal set; }
    public FrozenSet<string>? RequiredScopesServiceOwners { get; internal set; }
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
            var authorizer = context.RequestServices.GetRequiredService<ScopeAuthorizationService>();
            await authorizer.EnsureInitialized();

            var endpoint = context.GetEndpoint();
            if (endpoint is null)
            {
                throw new InvalidOperationException(
                    "Endpoint is null. Ensure the middleware is registered after routing"
                );
            }

            // Get scopes from endpoint metadata
            var scopeMetadata =
                endpoint.Metadata.GetMetadata<ScopeRequirementMetadata>() ?? authorizer.LookupMetadata(endpoint);
            var authenticated = context.RequestServices.GetRequiredService<IAuthenticationContext>().Current;
            var (errorMessageTextResourceKey, requiredScopes) = authenticated switch
            {
                Authenticated.User or Authenticated.SystemUser or Authenticated.Org => (
                    scopeMetadata?.ErrorMessageTextResourceKeyUser,
                    scopeMetadata?.RequiredScopesUsers
                ),
                Authenticated.ServiceOwner => (
                    scopeMetadata?.ErrorMessageTextResourceKeyServiceOwner,
                    scopeMetadata?.RequiredScopesServiceOwners
                ),
                _ => (null, null),
            };
            if (requiredScopes is not null)
            {
                var scopeClaim = user.FindFirst("urn:altinn:scope") ?? user.FindFirst("scope");
                var scopes = new Scopes(scopeClaim?.Value);
                if (!HasAnyScope(in scopes, requiredScopes))
                {
                    var lang = await authenticated.GetLanguage();
                    var translationService = context.RequestServices.GetRequiredService<ITranslationService>();
                    var errorMessage =
                        await translationService.TranslateTextKeyLenient(errorMessageTextResourceKey, lang)
                        ?? "Insufficient scope";

                    _logger.LogWarning(
                        "User does not have required scope for endpoint '{Endpoint}'",
                        ScopeAuthorizationService.GetEndpointDisplayName(endpoint)
                    );

                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(
                        new ProblemDetails
                        {
                            Title = "Forbidden",
                            Status = 403,
                            Detail = errorMessage,
                            Instance = context.Request.Path,
                        },
                        context.RequestAborted
                    );
                    return;
                }
                else
                {
                    _logger.LogDebug(
                        "User has required scope for endpoint '{Endpoint}'",
                        ScopeAuthorizationService.GetEndpointDisplayName(endpoint)
                    );
                }
            }
        }

        await _next(context);
    }

    private static bool HasAnyScope(in Scopes scopes, FrozenSet<string> requiredScopes)
    {
        Debug.Assert(requiredScopes.Count > 0);
        foreach (var scope in scopes)
        {
            if (requiredScopes.Contains(scope.ToString()))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class ScopeAuthorizationService(
    IAppConfigurationCache _appConfigurationCache,
    IEnumerable<EndpointDataSource> _endpointDataSources,
    IHostApplicationLifetime _hostLifetime,
    IOptions<GeneralSettings> _generalSettings,
    ILogger<ScopeAuthorizationService> _logger
) : IHostedService
{
    private readonly List<string> _endpointsToUserAuthorize = [];
    private readonly List<string> _endpointsNotUserAuthorized = [];
    private readonly List<string> _endpointsToServiceOwnerAuthorize = [];
    private readonly List<string> _endpointsNotServiceOwnerAuthorized = [];
    private readonly TaskCompletionSource _initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long _initializing;
    private CancellationTokenRegistration? _startedListener = null;

    public IReadOnlyList<string>? EndpointsToUserAuthorize => _endpointsToUserAuthorize;
    public IReadOnlyList<string>? EndpointsNotUserAuthorized => _endpointsNotUserAuthorized;
    public IReadOnlyList<string>? EndpointsToServiceOwnerAuthorize => _endpointsToServiceOwnerAuthorize;
    public IReadOnlyList<string>? EndpointsNotServiceOwnerAuthorized => _endpointsNotServiceOwnerAuthorized;

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

    private FrozenDictionary<string, ScopeRequirementMetadata>? _metadataLookup;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _startedListener = _hostLifetime.ApplicationStarted.Register(() =>
        {
            // Population of `EndpointsDataSource` begins _after_ `IHostedService.StartAsync` has is started,
            // but when the host lifetime is reported as started all the endpoints should already be there
            // since that means the HTTP server is ready to accept requests. So we can safely initialize here.
            // If we don't do this here, that means we can have an unlucky request in the beginning
            // that hits initialization.
            _ = Initialize();
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _startedListener?.Dispose();
        return Task.CompletedTask;
    }

    internal Task EnsureInitialized()
    {
        if (_initialization.Task.IsCompleted)
            return _initialization.Task;

        return Initialize();
    }

    internal ScopeRequirementMetadata LookupMetadata(Endpoint endpoint)
    {
        // We have this secondary lookup in case the metadata was not added to the endpoint.
        // All this code is supposed to work for all endpoints exposed throught ASP.NET Core,
        // but there is no global way to define metadata for endpoints (i.e. IEndpointConventionBuilder that applies to all endpoints)
        // So we have a fallback to this lookup which is also populated on initialization
        if (_metadataLookup is null)
            throw new InvalidOperationException("Scope authorization service is not initialized");

        if (endpoint.DisplayName is null)
            throw new InvalidOperationException("Endpoint display name is null");

        if (_metadataLookup.TryGetValue(endpoint.DisplayName, out var metadata))
            return metadata;

        throw new KeyNotFoundException($"No metadata found for endpoint '{endpoint.DisplayName}'");
    }

    private Task Initialize()
    {
        if (_generalSettings.Value.IsTest)
        {
            _initialization.TrySetResult();
            return _initialization.Task; // Skip initialization during WAF tests
        }
        if (_initialization.Task.IsCompleted)
            return _initialization.Task;

        if (Interlocked.CompareExchange(ref _initializing, 1, 0) != 0)
            return _initialization.Task; // Being initialized by another thread

        _logger.LogDebug("Starting scope authorization initialization");
        try
        {
            var appMetadata = _appConfigurationCache.ApplicationMetadata;
            var dedupe = new Dictionary<string, IEnumerable<string>>();

            if (
                !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.Users?.Read)
                || !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.Users?.Write)
                || !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.ServiceOwners?.Read)
                || !string.IsNullOrWhiteSpace(appMetadata.ApiScopes?.ServiceOwners?.Write)
            )
            {
                ProcessEndpoints(appMetadata, dedupe);
            }

            _initialization.TrySetResult();
            _logger.LogInformation(
                "Scope authorization initialized. Endpoints to authorize: {ToAuthorize}, Not authorized: {NotAuthorized}",
                _endpointsToUserAuthorize.Count,
                _endpointsNotUserAuthorized.Count
            );
        }
        catch (Exception ex)
        {
            _initialization.TrySetException(ex);
            _logger.LogError(ex, "Failed to initialize scope authorization service");
            throw;
        }

        return Task.CompletedTask;
    }

    private void ProcessEndpoints(ApplicationMetadata appMetadata, Dictionary<string, IEnumerable<string>> dedupe)
    {
        var metadataLookup = new Dictionary<string, ScopeRequirementMetadata>(StringComparer.Ordinal);
        foreach (var endpoint in _endpointDataSources.SelectMany(ed => ed.Endpoints))
        {
            var displayName = GetEndpointDisplayName(endpoint);
            var httpMethods = GetEndpointHttpMethods(endpoint);

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

            var metadata = endpoint.Metadata.GetMetadata<ScopeRequirementMetadata>() ?? new ScopeRequirementMetadata();

            // Using DisplayName on the endpoint here doesn't feel very solid,
            // but it's the only thing given by ASP.NET Core that I could find..
            // Atleast if there are endpoints that don't have this or have duplicates,
            // we will find out during initialization (the app will be obviously broken)
            if (endpoint.DisplayName is null)
                throw new InvalidOperationException("Endpoint display name is null");
            metadataLookup.Add(endpoint.DisplayName, metadata);

            var fallbackTextResourceKey =
                appMetadata.ApiScopes?.ErrorMessageTextResourceKey ?? "authorization.scopes.insufficient";
            metadata.ErrorMessageTextResourceKeyUser =
                appMetadata.ApiScopes?.Users?.ErrorMessageTextResourceKey ?? fallbackTextResourceKey;
            metadata.ErrorMessageTextResourceKeyServiceOwner =
                appMetadata.ApiScopes?.ServiceOwners?.ErrorMessageTextResourceKey ?? fallbackTextResourceKey;

            // Check for manual inclusion
            if (_manuallyIncludeActions.TryGetValue(displayName, out var scopeType))
            {
                ProcessEndpoint(metadata, scopeType, appMetadata.ApiScopes, displayName);
                continue;
            }

            // Determine scope type based on HTTP methods
            scopeType = httpMethods.All(m => _readHttpMethods.Contains(m)) ? ScopeType.Read : ScopeType.Write;

            // Check if endpoint should be authorized
            if (ShouldAuthorizeEndpoint(endpoint))
            {
                ProcessEndpoint(metadata, scopeType, appMetadata.ApiScopes, displayName);
            }
            else
            {
                _endpointsNotUserAuthorized.Add(displayName);
                _endpointsNotServiceOwnerAuthorized.Add(displayName);
            }
        }

        _metadataLookup = metadataLookup.ToFrozenDictionary(StringComparer.Ordinal);

        // Debug log endpoint authorization summary
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var authorizedEndpoints = string.Join(", ", _endpointsToUserAuthorize.Select(e => $"\n\t'{e}'"));
            var notAuthorizedEndpoints = string.Join(", ", _endpointsNotUserAuthorized.Select(e => $"\n\t'{e}'"));
            var serviceOwnerAuthorizedEndpoints = string.Join(
                ", ",
                _endpointsToServiceOwnerAuthorize.Select(e => $"\n\t'{e}'")
            );
            var serviceOwnerNotAuthorizedEndpoints = string.Join(
                ", ",
                _endpointsNotServiceOwnerAuthorized.Select(e => $"\n\t'{e}'")
            );

            // Collect actual computed scopes and error keys from endpoint metadata
            var allUserScopes = new HashSet<string>();
            var allServiceOwnerScopes = new HashSet<string>();
            var userErrorKeys = new HashSet<string>();
            var serviceOwnerErrorKeys = new HashSet<string>();

            foreach (var endpoint in _endpointDataSources.SelectMany(ed => ed.Endpoints))
            {
                var metadata = endpoint.Metadata.GetMetadata<ScopeRequirementMetadata>();
                if (metadata is not null)
                {
                    if (metadata.RequiredScopesUsers is not null)
                    {
                        foreach (var scope in metadata.RequiredScopesUsers)
                            allUserScopes.Add(scope);
                    }
                    if (metadata.RequiredScopesServiceOwners is not null)
                    {
                        foreach (var scope in metadata.RequiredScopesServiceOwners)
                            allServiceOwnerScopes.Add(scope);
                    }
                    if (!string.IsNullOrEmpty(metadata.ErrorMessageTextResourceKeyUser))
                        userErrorKeys.Add(metadata.ErrorMessageTextResourceKeyUser);
                    if (!string.IsNullOrEmpty(metadata.ErrorMessageTextResourceKeyServiceOwner))
                        serviceOwnerErrorKeys.Add(metadata.ErrorMessageTextResourceKeyServiceOwner);
                }
            }

            _logger.LogDebug(
                "Endpoint API scope authorization summary:\n"
                    + "User-authorized endpoints ({UserAuthorizedCount}): [{UserAuthorized}]\n"
                    + "User-not-authorized endpoints ({UserNotAuthorizedCount}): [{UserNotAuthorized}]\n"
                    + "ServiceOwner-authorized endpoints ({ServiceOwnerAuthorizedCount}): [{ServiceOwnerAuthorized}]\n"
                    + "ServiceOwner-not-authorized endpoints ({ServiceOwnerNotAuthorizedCount}): [{ServiceOwnerNotAuthorized}]\n"
                    + "User scopes: [{UserScopes}]\n"
                    + "Service owner scopes: [{ServiceOwnerScopes}]\n"
                    + "User error message keys: [{UserErrorKeys}]\n"
                    + "Service owner error message keys: [{ServiceOwnerErrorKeys}]",
                _endpointsToUserAuthorize.Count,
                authorizedEndpoints,
                _endpointsNotUserAuthorized.Count,
                notAuthorizedEndpoints,
                _endpointsToServiceOwnerAuthorize.Count,
                serviceOwnerAuthorizedEndpoints,
                _endpointsNotServiceOwnerAuthorized.Count,
                serviceOwnerNotAuthorizedEndpoints,
                string.Join(", ", allUserScopes.Order()),
                string.Join(", ", allServiceOwnerScopes.Order()),
                string.Join(", ", userErrorKeys.Order()),
                string.Join(", ", serviceOwnerErrorKeys.Order())
            );
        }
    }

    private void ProcessEndpoint(
        ScopeRequirementMetadata metadata,
        ScopeType scopeType,
        ApiScopesConfiguration? apiScopes,
        string displayName
    )
    {
        var usersScopeSet = CreateUsersScopesSet(scopeType, apiScopes?.Users);
        if (usersScopeSet is not null)
        {
            _endpointsToUserAuthorize.Add(displayName);
            metadata.RequiredScopesUsers = usersScopeSet;
        }
        else
        {
            _endpointsNotUserAuthorized.Add(displayName);
        }

        var serviceOwnersScopeSet = CreateServiceOwnersScopesSet(scopeType, apiScopes?.ServiceOwners);
        if (serviceOwnersScopeSet is not null)
        {
            _endpointsToServiceOwnerAuthorize.Add(displayName);
            metadata.RequiredScopesServiceOwners = serviceOwnersScopeSet;
        }
        else
        {
            _endpointsNotServiceOwnerAuthorized.Add(displayName);
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

    private static bool ShouldAuthorizeEndpoint(Endpoint endpoint)
    {
        // Skip endpoints with AllowAnonymous
        if (endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null)
            return false;

        // Check route parameters for instance-related endpoints
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            var routePattern = routeEndpoint.RoutePattern;
            var hasInstanceParameters = routePattern.Parameters.Any(p =>
                string.Equals(p.Name, "instanceGuid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Name, "instanceId", StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.Name, "instanceOwnerPartyId", StringComparison.OrdinalIgnoreCase)
            );

            return hasInstanceParameters;
        }

        return true;
    }

    private FrozenSet<string>? CreateUsersScopesSet(ScopeType type, ApiScopes? apiScopes)
    {
        var configuredScope = type == ScopeType.Read ? apiScopes?.Read : apiScopes?.Write;
        if (string.IsNullOrWhiteSpace(configuredScope))
            return null;

        var appMetadata = _appConfigurationCache.ApplicationMetadata;
        configuredScope = configuredScope.Replace("[app]", appMetadata.AppIdentifier.App);
        return new[] { configuredScope, "altinn:portal/enduser" }.ToFrozenSet(StringComparer.Ordinal);
    }

    private FrozenSet<string>? CreateServiceOwnersScopesSet(ScopeType type, ApiScopes? apiScopes)
    {
        var configuredScope = type == ScopeType.Read ? apiScopes?.Read : apiScopes?.Write;
        if (string.IsNullOrWhiteSpace(configuredScope))
            return null;

        var appMetadata = _appConfigurationCache.ApplicationMetadata;
        configuredScope = configuredScope.Replace("[app]", appMetadata.AppIdentifier.App);
        return new[] { configuredScope }.ToFrozenSet(StringComparer.Ordinal);
    }
}

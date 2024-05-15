using System.Collections.Frozen;
using System.Security.Claims;
using AltinnCore.Authentication.Constants;
using Microsoft.AspNetCore.Http.Features;

namespace Altinn.App.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware for adding telemetry to the request.
/// </summary>
public class TelemetryEnrichingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TelemetryEnrichingMiddleware> _logger;
    private static readonly FrozenDictionary<string, Action<Claim, TelemetryEnrichingMiddleware>> ClaimActions;

    private string? userName;
    private int? userId;
    private int? partyId;
    private int? authenticationLevel;

    static TelemetryEnrichingMiddleware()
    {
        var actions = new Dictionary<string, Action<Claim, TelemetryEnrichingMiddleware>>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            { AltinnCoreClaimTypes.UserName, (claim, middleware) => middleware.userName = claim.Value },
            {
                AltinnCoreClaimTypes.UserId,
                (claim, middleware) =>
                {
                    if (int.TryParse(claim.Value, out var result))
                    {
                        middleware.userId = result;
                    }
                }
            },
            {
                AltinnCoreClaimTypes.PartyID,
                (claim, middleware) =>
                {
                    if (int.TryParse(claim.Value, out var result))
                    {
                        middleware.partyId = result;
                    }
                }
            },
            {
                AltinnCoreClaimTypes.AuthenticationLevel,
                (claim, middleware) =>
                {
                    if (int.TryParse(claim.Value, out var result))
                    {
                        middleware.authenticationLevel = result;
                    }
                }
            }
        };

        ClaimActions = actions.ToFrozenDictionary();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryEnrichingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public TelemetryEnrichingMiddleware(RequestDelegate next, ILogger<TelemetryEnrichingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var trace = context.Features.Get<IHttpActivityFeature>();
        if (trace is null)
        {
            await _next(context);
            return;
        }

        try
        {
            foreach (var claim in context.User.Claims)
            {
                if (ClaimActions.TryGetValue(claim.Type, out var action))
                {
                    action(claim, this);
                }
            }

            trace.Activity.SetTag(Core.Features.Telemetry.Labels.UserName, userName);
            trace.Activity.SetTag(Core.Features.Telemetry.Labels.UserId, userId);
            trace.Activity.SetTag(Core.Features.Telemetry.Labels.UserPartyId, partyId);
            trace.Activity.SetTag(Core.Features.Telemetry.Labels.UserAuthenticationLevel, authenticationLevel);

            // Set telemetry tags with route values if available.
            if (
                context.Request.RouteValues.TryGetValue("instanceOwnerPartyId", out var instanceOwnerPartyId)
                && instanceOwnerPartyId != null
                && int.TryParse(instanceOwnerPartyId.ToString(), out var instanceOwnerPartyIdInt)
            )
            {
                trace.Activity.SetTag(Core.Features.Telemetry.Labels.InstanceOwnerPartyId, instanceOwnerPartyIdInt);
            }

            if (context.Request.RouteValues.TryGetValue("instanceGuid", out var instanceGuid) && instanceGuid != null)
            {
                trace.Activity.SetTag(Core.Features.Telemetry.Labels.InstanceGuid, instanceGuid);
            }

            if (context.Request.RouteValues.TryGetValue("dataGuid", out var dataGuid) && dataGuid != null)
            {
                trace.Activity.SetTag(Core.Features.Telemetry.Labels.DataGuid, dataGuid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while enriching telemetry.");
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for adding the <see cref="TelemetryEnrichingMiddleware"/> to the application pipeline.
/// </summary>
public static class TelemetryEnrichingMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="TelemetryEnrichingMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static IApplicationBuilder UseTelemetryEnricher(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TelemetryEnrichingMiddleware>(
            app.ApplicationServices.GetRequiredService<ILogger<TelemetryEnrichingMiddleware>>()
        );
    }
}

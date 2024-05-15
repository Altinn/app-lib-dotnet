using System.Collections.Frozen;
using System.Diagnostics;
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
    private static readonly FrozenDictionary<string, Action<Claim, Activity>> ClaimActions;

    static TelemetryEnrichingMiddleware()
    {
        var actions = new Dictionary<string, Action<Claim, Activity>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                AltinnCoreClaimTypes.UserName,
                (claim, activity) => activity.SetTag(Core.Features.Telemetry.Labels.UserName, claim.Value)
            },
            {
                AltinnCoreClaimTypes.UserId,
                (claim, activity) =>
                {
                    if (int.TryParse(claim.Value, out var result))
                    {
                        activity.SetTag(Core.Features.Telemetry.Labels.UserId, result);
                    }
                }
            },
            {
                AltinnCoreClaimTypes.PartyID,
                (claim, activity) =>
                {
                    if (int.TryParse(claim.Value, out var result))
                    {
                        activity.SetTag(Core.Features.Telemetry.Labels.UserPartyId, result);
                    }
                }
            },
            {
                AltinnCoreClaimTypes.AuthenticationLevel,
                (claim, activity) =>
                {
                    if (int.TryParse(claim.Value, out var result))
                    {
                        activity.SetTag(Core.Features.Telemetry.Labels.UserAuthenticationLevel, result);
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
            var activity = trace.Activity;

            foreach (var claim in context.User.Claims)
            {
                if (ClaimActions.TryGetValue(claim.Type, out var action))
                {
                    action(claim, activity);
                }
            }

            // Set telemetry tags with route values if available.
            if (
                context.Request.RouteValues.TryGetValue("instanceOwnerPartyId", out var instanceOwnerPartyId)
                && instanceOwnerPartyId != null
                && int.TryParse(instanceOwnerPartyId.ToString(), out var instanceOwnerPartyIdInt)
            )
            {
                activity.SetTag(Core.Features.Telemetry.Labels.InstanceOwnerPartyId, instanceOwnerPartyIdInt);
            }

            if (context.Request.RouteValues.TryGetValue("instanceGuid", out var instanceGuid) && instanceGuid != null)
            {
                activity.SetTag(Core.Features.Telemetry.Labels.InstanceGuid, instanceGuid);
            }

            if (context.Request.RouteValues.TryGetValue("dataGuid", out var dataGuid) && dataGuid != null)
            {
                activity.SetTag(Core.Features.Telemetry.Labels.DataGuid, dataGuid);
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

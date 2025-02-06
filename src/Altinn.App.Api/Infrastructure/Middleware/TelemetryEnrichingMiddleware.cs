using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Microsoft.AspNetCore.Http.Features;

namespace Altinn.App.Api.Infrastructure.Middleware;

internal sealed class TelemetryEnrichingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TelemetryEnrichingMiddleware> _logger;

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
        var activity = context.Features.Get<IHttpActivityFeature>()?.Activity;
        if (activity is null)
        {
            await _next(context);
            return;
        }

        try
        {
            var authenticationContext = context.RequestServices.GetRequiredService<IAuthenticationContext>();
            var currentAuth = authenticationContext.Current;
            activity.SetAuthenticated(currentAuth);

            // Set telemetry tags with route values if available.
            if (
                context.Request.RouteValues.TryGetValue("instanceOwnerPartyId", out var instanceOwnerPartyId)
                && instanceOwnerPartyId != null
                && int.TryParse(instanceOwnerPartyId.ToString(), out var instanceOwnerPartyIdInt)
            )
            {
                activity.SetInstanceOwnerPartyId(instanceOwnerPartyIdInt);
            }

            var routeValues = context.Request.RouteValues;
            if (
                routeValues.TryGetValue("instanceGuid", out var instanceGuidObj) && instanceGuidObj is Guid instanceGuid
            )
            {
                activity.SetInstanceId(instanceGuid);
            }

            if (routeValues.TryGetValue("dataGuid", out var dataGuidObj) && dataGuidObj is Guid dataGuid)
            {
                activity.SetDataElementId(dataGuid);
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

using System.Security.Claims;
using AltinnCore.Authentication.Constants;
using Microsoft.AspNetCore.Http.Features;

namespace Altinn.App.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware for adding telemetry to the request
/// </summary>
public static class TelemetryEnrichingMiddleware
{
    /// <summary>
    /// Adds telemetry to the request
    /// </summary>
    /// <param name="app">App</param>
    public static IApplicationBuilder UseTelemetryEnricher(this IApplicationBuilder app)
    {
        app.Use(
            static async (context, next) =>
            {
                var trace = context.Features.Get<IHttpActivityFeature>();
                if (trace is null)
                {
                    await next(context);
                    return;
                }

                string? userName = null;
                int? userId = null;
                int? partyId = null;
                int? authenticationLevel = null;
                foreach (Claim claim in context.User.Claims)
                {
                    if (claim.Type.Equals(AltinnCoreClaimTypes.UserName))
                    {
                        userName = claim.Value;
                    }
                    else if (claim.Type.Equals(AltinnCoreClaimTypes.UserId))
                    {
                        userId = Convert.ToInt32(claim.Value);
                    }
                    else if (claim.Type.Equals(AltinnCoreClaimTypes.PartyID))
                    {
                        partyId = Convert.ToInt32(claim.Value);
                    }
                    else if (claim.Type.Equals(AltinnCoreClaimTypes.AuthenticationLevel))
                    {
                        authenticationLevel = Convert.ToInt32(claim.Value);
                    }
                }

                trace.Activity.SetTag("user.name", userName);
                trace.Activity.SetTag("user.id", userId);
                trace.Activity.SetTag("user.party_id", partyId);
                trace.Activity.SetTag("user.authentication_level", authenticationLevel);

                if (
                    context.Request.RouteValues.TryGetValue("instanceOwnerPartyId", out var instanceOwnerPartyId)
                    && instanceOwnerPartyId is not null
                )
                {
                    if (instanceOwnerPartyId is not int instanceOwnerPartyIdInt)
                    {
                        instanceOwnerPartyIdInt = Convert.ToInt32(instanceOwnerPartyId);
                    }

                    trace.Activity.SetTag(Core.Features.Telemetry.Labels.InstanceOwnerPartyId, instanceOwnerPartyIdInt);
                }

                if (
                    context.Request.RouteValues.TryGetValue("instanceGuid", out var instanceGuid)
                    && instanceGuid is not null
                )
                {
                    trace.Activity.SetTag(Core.Features.Telemetry.Labels.InstanceGuid, instanceGuid);
                }

                if (context.Request.RouteValues.TryGetValue("dataGuid", out var dataGuid) && dataGuid is not null)
                {
                    trace.Activity.SetTag(Core.Features.Telemetry.Labels.DataGuid, dataGuid);
                }

                await next(context);
            }
        );

        return app;
    }
}

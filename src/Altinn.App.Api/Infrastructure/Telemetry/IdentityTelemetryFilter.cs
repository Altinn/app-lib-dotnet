#nullable disable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Altinn.App.Core.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Altinn.App.Api.Infrastructure.Telemetry;

/// <summary>
/// Filter to enrich request telemetry with identity information
/// </summary>
[ExcludeFromCodeCoverage]
public class IdentityTelemetryFilter : ITelemetryProcessor
{
    private ITelemetryProcessor _next { get; set; }

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityTelemetryFilter"/> class.
    /// </summary>
    public IdentityTelemetryFilter(ITelemetryProcessor next, IHttpContextAccessor httpContextAccessor)
    {
        _next = next;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public void Process(ITelemetry item)
    {
        RequestTelemetry request = item as RequestTelemetry;

        if (request != null)
        {
            HttpContext ctx = _httpContextAccessor.HttpContext;

            if (ctx?.User != null)
            {
                int? orgNumber = ctx.User.GetOrgNumber();
                int? userId = ctx.User.GetUserIdAsInt();
                int? partyId = ctx.User.GetPartyIdAsInt();
                int authLevel = ctx.User.GetAuthenticationLevel();

                request.Properties.Add("partyId", partyId?.ToString(CultureInfo.InvariantCulture) ?? "");
                request.Properties.Add("authLevel", authLevel.ToString(CultureInfo.InvariantCulture));

                if (userId != null)
                {
                    request.Properties.Add("userId", userId?.ToString(CultureInfo.InvariantCulture) ?? "");
                }

                if (orgNumber != null)
                {
                    request.Properties.Add("orgNumber", orgNumber?.ToString(CultureInfo.InvariantCulture) ?? "");
                }
            }
        }

        _next.Process(item);
    }
}

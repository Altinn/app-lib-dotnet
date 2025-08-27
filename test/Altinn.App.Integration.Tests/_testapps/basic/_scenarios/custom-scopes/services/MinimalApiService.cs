using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestApp.Shared;

namespace TestScenario.MinimalApiScopes;

public static class MinimalApiService
{
    public static void RegisterServices(IServiceCollection services)
    {
        // Register the endpoint configurator that will add minimal API endpoints
        services.AddSingleton<IEndpointConfigurator, MinimalApiEndpointConfigurator>();
    }
}

public class MinimalApiEndpointConfigurator : IEndpointConfigurator
{
    public void ConfigureEndpoints(WebApplication app)
    {
        // Minimal API endpoints that should be protected by scopes

        // GET endpoint with instanceGuid - should be protected with read scope
        app.MapGet(
                "/{org}/{app}/api/instances/{instanceGuid}/minimal-data",
                (Guid instanceGuid) => Results.Ok(new { instanceGuid, data = "minimal-api-read-data" })
            )
            .WithName("GetMinimalInstanceData");

        // POST endpoint with instanceGuid - should be protected with write scope
        app.MapPost(
                "/{org}/{app}/api/instances/{instanceGuid}/minimal-process",
                (Guid instanceGuid, object data) => Results.Ok(new { instanceGuid, processed = true })
            )
            .WithName("PostMinimalInstanceProcess");

        // GET endpoint with instanceOwnerPartyId - should be protected with read scope
        app.MapGet(
                "/{org}/{app}/api/parties/{instanceOwnerPartyId}/minimal-summary",
                (int instanceOwnerPartyId) => Results.Ok(new { instanceOwnerPartyId, summary = "minimal-summary" })
            )
            .WithName("GetMinimalPartySummary");

        // Anonymous endpoint - should NOT be protected
        app.MapGet("/{org}/{app}/api/minimal-public", () => Results.Ok(new { message = "public endpoint" }))
            .AllowAnonymous()
            .WithName("GetMinimalPublic");
    }
}

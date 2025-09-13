using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestApp.Shared;

namespace TestScenario.MinimalApiScopes;

public static class Apis
{
    public static void RegisterServices(IServiceCollection services)
    {
        // Register the endpoint configurator that will add minimal API endpoints
        services.AddSingleton<IEndpointConfigurator, ApiEndpoints>();
    }
}

internal sealed class ApiEndpoints : IEndpointConfigurator
{
    public void ConfigureEndpoints(WebApplication app)
    {
        // Minimal API endpoints that should be protected by scopes

        // GET endpoint with instanceGuid - should be protected with read scope
        app.MapGet(
                "/{org}/{app}/api/testing/{instanceGuid:guid}",
                (Guid instanceGuid) => Results.Ok(new { instanceGuid })
            )
            .WithName("API testing - GET - instanceGuid");

        // POST endpoint with instanceGuid - should be protected with write scope
        app.MapPost(
                "/{org}/{app}/api/testing/{instanceGuid:guid}",
                (Guid instanceGuid, object data) => Results.Ok(new { instanceGuid })
            )
            .WithName("API testing - POST - instanceGuid");

        // GET endpoint with instanceOwnerPartyId - should be protected with read scope
        app.MapGet(
                "/{org}/{app}/api/testing/{instanceOwnerPartyId:int}",
                (int instanceOwnerPartyId) => Results.Ok(new { instanceOwnerPartyId })
            )
            .WithName("API testing - GET - instanceOwnerPartyId");

        // POST endpoint with instanceOwnerPartyId - should be protected with write scope
        app.MapPost(
                "/{org}/{app}/api/testing/{instanceOwnerPartyId:int}",
                (int instanceOwnerPartyId) => Results.Ok(new { instanceOwnerPartyId })
            )
            .WithName("API testing - POST - instanceOwnerPartyId");

        // Anonymous endpoint - should NOT be protected
        app.MapGet("/{org}/{app}/api/testing/public", () => Results.Ok(new { message = "public endpoint" }))
            .AllowAnonymous()
            .WithName("API testing - GET - public");
    }
}

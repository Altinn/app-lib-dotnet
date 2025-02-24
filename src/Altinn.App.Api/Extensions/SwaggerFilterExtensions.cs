using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.App.Api.Extensions;

internal static class SwaggerFilterExtensions
{
    /// <summary>
    /// Adds a filter to the swagger documentation to remove paths that are not used.
    /// </summary>
    /// <param name="services"></param>
    public static void AddSwaggerFilter(this IServiceCollection services)
    {
        services.Configure<SwaggerGenOptions>(c =>
        {
            c.DocumentFilter<DocumentFilter>();
        });
    }
}

internal class DocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Remove path from swagger that is used only for backwards compatibility.
        if (!swaggerDoc.Paths.Remove("/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataType}"))
        {
            throw new InvalidOperationException(
                "Path /{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataType} not found in swagger document."
            );
        }

        if (
            !swaggerDoc.Paths.Remove(
                "/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/type/{dataType}"
            )
        )
        {
            throw new InvalidOperationException(
                "Path /{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/type/{dataType} not found in swagger document."
            );
        }
    }
}

using Altinn.App.Core.Helpers;
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
        swaggerDoc.Paths.Remove("/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataType}");

        swaggerDoc.Paths.Remove(
            "/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/type/{dataType}"
        );

        // Remove the dataType parameter from the route where it does not apply.
        // The previous lines removed the paths that used the dataType parameter.
        swaggerDoc
            .Paths["/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}"]
            .Operations.Values.ToList()
            .ForEach(o => o.Parameters.RemoveAll(p => p.Name == "dataType" && p.In == ParameterLocation.Path));
    }
}

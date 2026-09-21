using Altinn.App.Api.Controllers;
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
        swaggerDoc.Info.Description = CustomOpenApiController.InfoDescriptionWarningText;
        // Remove path from swagger that is used only for backwards compatibility.
        swaggerDoc.Paths.Remove("/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataType}");

        swaggerDoc.Paths.Remove(
            "/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}/type/{dataType}"
        );

        RemovePathParametersMissingFromTemplate(swaggerDoc);
    }

    /// <summary>
    /// Swashbuckle emits every [FromRoute] action parameter as a path parameter on all route templates of the action,
    /// including templates that do not contain the parameter (for example <c>dataType</c> on
    /// <c>/data/{dataGuid}</c>, which is only present in the alias template removed above).
    /// A path parameter that is not part of the template is invalid OpenAPI, so remove those.
    /// </summary>
    private static void RemovePathParametersMissingFromTemplate(OpenApiDocument swaggerDoc)
    {
        foreach (var (path, pathItem) in swaggerDoc.Paths)
        {
            foreach (var operation in pathItem.Operations.Values)
            {
                for (int i = operation.Parameters.Count - 1; i >= 0; i--)
                {
                    var parameter = operation.Parameters[i];
                    if (
                        parameter.In == ParameterLocation.Path
                        && !path.Contains($"{{{parameter.Name}}}", StringComparison.Ordinal)
                    )
                    {
                        operation.Parameters.RemoveAt(i);
                    }
                }
            }
        }
    }
}

namespace Altinn.App.Api.Controllers;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Altinn.App.Api.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Swashbuckle.AspNetCore.SwaggerGen;
using DataType = Altinn.Platform.Storage.Interface.Models.DataType;

/// <summary>
/// Generate custom OpenAPI documentation for the app that includes all the data types and operations
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class CustomOpenApiController : Controller
{
    private readonly IAppMetadata _appMetadata;
    private readonly IAppModel _appModel;
    private readonly SchemaGenerator _schemaGenerator;
    private readonly SchemaRepository _schemaRepository;

    /// <summary>
    /// Constructor with services from dependency injection
    /// </summary>
    public CustomOpenApiController(
        IAppModel appModel,
        IAppMetadata appMetadata,
        ISerializerDataContractResolver dataContractResolver,
        IOptions<MvcOptions> mvcOptions
    )
    {
        _appModel = appModel;
        _appMetadata = appMetadata;
        _schemaGenerator = new SchemaGenerator(new SchemaGeneratorOptions(), dataContractResolver, mvcOptions);
        _schemaRepository = new SchemaRepository();
    }

    /// <summary>
    /// Generate the custom OpenAPI documentation for the app
    /// </summary>
    [HttpGet("/{org}/{app}/v1/customOpenapi.json")]
    public async Task<ActionResult> Index()
    {
        var appMetadata = await _appMetadata.GetApplicationMetadata();
        var document = new OpenApiDocument()
        {
            Info = new()
            {
                Title =
                    "Altinn 3 App API for "
                    + (
                        appMetadata.Title.TryGetValue("en", out var englishTitle) ? englishTitle
                        : appMetadata.Title.TryGetValue("nb", out var norwegianTitle) ? norwegianTitle
                        : "Unknown"
                    ),
                Contact = new()
                {
                    Name = "Digitaliseringsdirektoratet (altinn)",
                    Url = new("https://altinn.slack.com"),
                },
                Version = appMetadata.AltinnNugetVersion,
                Description = GetIntroDoc(appMetadata),
            },
            ExternalDocs = new() { Description = "Altinn 3 Documentation", Url = new("https://docs.altinn.studio") },
            Paths = [], // Add to this later
            Components = new OpenApiComponents()
            {
                Schemas = _schemaRepository.Schemas,
                SecuritySchemes = { ["AltinnToken"] = Snippets.AltinnTokenSecurityScheme },
                Responses = { ["ProblemDetails"] = Snippets.ProblemDetailsResponseSchema },
                Parameters = Snippets.ComponentParameters,
            },
            SecurityRequirements = [new OpenApiSecurityRequirement() { [Snippets.AltinnTokenSecurityScheme] = [] }],
            Servers =
            {
                new OpenApiServer() { Url = $"http://local.altinn.cloud", Description = "Local development server" },
                new OpenApiServer()
                {
                    Url = $"https://{appMetadata.Org}.apps.tt02.altinn.no",
                    Description = "TT02 server",
                },
                new OpenApiServer()
                {
                    Url = $"https://{appMetadata.Org}.apps.altinn.no",
                    Description = "Production server",
                },
            },
        };

        AddCommonRoutes(document, appMetadata);

        foreach (var dataType in appMetadata.DataTypes)
        {
            AddRoutsForDataType(document, appMetadata, dataType);
        }

        // Fix issues in the schemas
        var walker = new OpenApiWalker(new SchemaPostVisitor());
        walker.Walk(document);

        return Ok(document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json));
    }

    private static string GetIntroDoc(ApplicationMetadata appMetadata)
    {
        StringBuilder sb = new();
        sb.AppendLine("This is the API for an Altinn 3 app. The API is based on the OpenAPI 3.0 specification.");
        sb.AppendLine("This app has the following data types:");
        sb.AppendLine("| DataTypeId | Type | Allowed number | MimeTypes | TaskId |");
        sb.AppendLine("|------------|------|----------------|-----------|--------|");
        foreach (var dataType in appMetadata.DataTypes)
        {
            sb.Append('|');
            sb.Append(dataType.Id);
            sb.Append('|');
            if (dataType.AppLogic?.ClassRef is null)
            {
                sb.Append("Attachment");
            }
            else
            {
                sb.Append("FormData");
                if (dataType.AppLogic?.AutoCreate == true)
                {
                    sb.Append(" (AutoCreate)");
                }
            }
            sb.Append('|');
            if (dataType.MaxCount == dataType.MinCount)
            {
                if (dataType.MaxCount == 0)
                {
                    sb.Append('-');
                }
                else
                {
                    sb.Append(dataType.MaxCount);
                }
            }
            else if (dataType.MaxCount > 0 && dataType.MinCount > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $"{dataType.MinCount}-{dataType.MaxCount}");
            }
            else if (dataType.MaxCount > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $"0-{dataType.MaxCount}");
            }
            else if (dataType.MinCount > 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $"{dataType.MinCount}-∞");
            }
            else
            {
                sb.Append('-');
            }
            sb.Append('|');
            if (dataType.AllowedContentTypes is not null)
            {
                sb.Append(string.Join(", ", dataType.AllowedContentTypes));
            }
            else
            {
                sb.Append('*');
            }
            sb.Append('|');
            sb.Append(dataType.TaskId);
            sb.Append('|');
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private void AddCommonRoutes(OpenApiDocument document, ApplicationMetadata appMetadata)
    {
        OpenApiTag[] instanceTags = [new() { Name = "Instances", Description = "Operations on instances" }];
        var instanceSchema = _schemaGenerator.GenerateSchema(typeof(Instance), _schemaRepository);
        document.Components.Schemas.Add("InstanceWrite", Snippets.InstanceWriteSchema);
        var instanceWriteSchema = new OpenApiSchema()
        {
            Reference = new OpenApiReference() { Id = "InstanceWrite", Type = ReferenceType.Schema },
        };

        OpenApiMediaType multipartMediaType = new OpenApiMediaType()
        {
            Schema = new OpenApiSchema() { Type = "object", Properties = { ["instance"] = instanceWriteSchema } },
            Encoding = { ["instance"] = new OpenApiEncoding() { ContentType = "application/json" } },
        };
        foreach (var dataType in appMetadata.DataTypes)
        {
            multipartMediaType.Schema.Properties.Add(
                dataType.Id,
                new OpenApiSchema() { Type = "string", Format = "binary" }
            );
            multipartMediaType.Encoding.Add(
                dataType.Id,
                new OpenApiEncoding()
                {
                    ContentType = dataType.AllowedContentTypes is [.. var contentTypes]
                        ? string.Join(' ', contentTypes)
                        : "application/octet-stream",
                }
            );
        }
        document.Paths.Add(
            $"/{appMetadata.Id}/instances",
            new OpenApiPathItem()
            {
                Summary = "Operations for instances",
                Description = "CRUD operations for instances",
                Servers = [new OpenApiServer() { Url = $"/{appMetadata.Id}" }],
                Operations =
                {
                    [OperationType.Post] = new OpenApiOperation()
                    {
                        Tags = instanceTags,
                        Summary = "Create new instance",
                        Description = "The main api for creating new instances. ",
                        Parameters =
                        {
                            new OpenApiParameter(Snippets.InstanceOwnerPartyIdParameterReference)
                            {
                                // Use snippet, but override
                                Description =
                                    "The party id of the instance owner (use either this or an instance document in the body)",
                                In = ParameterLocation.Query,
                                Required = false,
                            },
                            Snippets.LanguageParameterReference,
                        },
                        RequestBody = new OpenApiRequestBody()
                        {
                            Required = false,
                            Description = """
                            Instance document, formData and attachments

                            Any mime type that is not ``"application/json"`` or ``"multipart/form-data"`` with an instance document
                            will require the ``instanceOwnerPartyId`` parameter. Otherwise you must use the simplified instance document to specify instance owner.
                            Either using ``instanceOwner.partyId`` or ``instanceOwner.personNumber`` or ``instanceOwner.organisationNumber`` (or ``instanceOwner.username`` see [app-lib-dotnet/#652](https://github.com/Altinn/app-lib-dotnet/issues/652)).
                            """,
                            Content =
                            {
                                ["empty"] = new OpenApiMediaType()
                                {
                                    Schema = new OpenApiSchema() { Type = "", Example = new OpenApiString("") },
                                },
                                ["application/json"] = new OpenApiMediaType() { Schema = instanceWriteSchema },
                                ["multipart/form-data"] = multipartMediaType,
                            },
                        },
                        Responses = Snippets.AddCommonErrorResponses(
                            HttpStatusCode.Created,
                            new OpenApiResponse()
                            {
                                Description = "Instance created",
                                Content = { ["application/json"] = new OpenApiMediaType() { Schema = instanceSchema } },
                            }
                        ),
                    },
                },
            }
        );
        document.Paths.Add(
            $"/{appMetadata.Id}/instances/{{instanceOwnerPartyId}}/{{instanceGuid}}",
            new OpenApiPathItem()
            {
                Summary = "Operations for instance",
                Description = "CRUD operations for a specific instance",
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation()
                    {
                        Tags = instanceTags,
                        OperationId = "GetInstance",
                        Summary = "Get instance",
                        Description = "Get a specific instance",
                        Responses = new()
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Description = "Instance found",
                                Content = { ["application/json"] = new OpenApiMediaType() { Schema = instanceSchema } },
                            },
                            ["404"] = new OpenApiResponse() { Description = "Instance not found" },
                        },
                    },
                    [OperationType.Delete] = new OpenApiOperation()
                    {
                        Tags = instanceTags,
                        Summary = "Delete instance",
                        Description = "Delete a specific instance",
                        Responses = new()
                        {
                            ["204"] = new OpenApiResponse() { Description = "Instance deleted" },
                            ["404"] = new OpenApiResponse() { Description = "Instance not found" },
                        },
                    },
                    [OperationType.Patch] = new OpenApiOperation()
                    {
                        Tags = instanceTags,
                        Summary = "Patch data elements on instance",
                        RequestBody = new OpenApiRequestBody()
                        {
                            Required = true,
                            Content =
                            {
                                ["application/json"] = new OpenApiMediaType()
                                {
                                    Schema = Snippets.PatchSchema,
                                    // Schema = _schemaGenerator.GenerateSchema(
                                    //     typeof(DataPatchRequestMultiple),
                                    //     _schemaRepository
                                    // ),
                                },
                            },
                        },
                        Responses = new()
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Description = "Data elements patched",
                                Content =
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = _schemaGenerator.GenerateSchema(
                                            typeof(DataPatchResponseMultiple),
                                            _schemaRepository
                                        ),
                                    },
                                },
                            },
                            ["404"] = new OpenApiResponse() { Description = "Instance not found" },
                        },
                    },
                },
                Parameters =
                [
                    Snippets.InstanceOwnerPartyIdParameterReference,
                    Snippets.InstanceGuidParameterReference,
                    Snippets.LanguageParameterReference,
                ],
            }
        );

        document.Paths.Add(
            $"/{appMetadata.Id}/instances/{{instanceOwnerPartyId}}/{{instanceGuid}}/data/{{dataGuid}}",
            new OpenApiPathItem()
            {
                Summary = $"Delete data element",
                Operations =
                {
                    [OperationType.Delete] = new OpenApiOperation()
                    {
                        Tags = instanceTags,
                        Summary = "Delete data element",
                        Description = "Delete data for a specific data element",
                        Responses = new()
                        {
                            ["204"] = new OpenApiResponse() { Description = "Data deleted" },
                            ["404"] = new OpenApiResponse() { Description = "Data not found" },
                        },
                    },
                },
                Parameters =
                [
                    Snippets.InstanceOwnerPartyIdParameterReference,
                    Snippets.InstanceGuidParameterReference,
                    Snippets.DataGuidParameterReference,
                ],
            }
        );

        var commonTags = new[]
        {
            new OpenApiTag() { Name = "Static", Description = "Static info about the application" },
        };

        document.Paths.Add(
            $"/{appMetadata.Id}/api/v1/applicationmetadata",
            new OpenApiPathItem()
            {
                Summary = "Get application metadata",
                Description = "Get the metadata for the application",
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation()
                    {
                        Tags = commonTags,
                        Summary = "Get application metadata",
                        Description = "Get the metadata for the application",
                        Security = { },
                        Responses = new()
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Description = "Application metadata found",
                                Content =
                                {
                                    ["application/json"] = new OpenApiMediaType()
                                    {
                                        Schema = _schemaGenerator.GenerateSchema(
                                            typeof(ApplicationMetadata),
                                            _schemaRepository
                                        ),
                                    },
                                },
                            },
                        },
                    },
                },
            }
        );
        // Auth exchange endpoint
        var authTags = new[]
        {
            new OpenApiTag()
            {
                Name = "Authentication",
                Description = "Operations for exchanging maskinporten or idporten tokens to altinn tokens",
            },
        };
        document.Paths.Add(
            "/authentication/api/v1/exchange/{tokenProvider}",
            new OpenApiPathItem()
            {
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation()
                    {
                        Tags = authTags,
                        Summary =
                            "Action for exchanging a JWT generated by a trusted token provider with a new JWT for further use as authentication against rest of Altinn.",
                        Parameters =
                        {
                            new OpenApiParameter()
                            {
                                Name = "tokenProvider",
                                In = ParameterLocation.Path,
                                Required = true,
                                Schema = new OpenApiSchema()
                                {
                                    Type = "string",
                                    Enum = [new OpenApiString("maskinporten"), new OpenApiString("id-porten")],
                                },
                            },
                            new OpenApiParameter()
                            {
                                Name = "Autorization",
                                Description =
                                    "Bearer token from the selected token provider to exchange for an altinn token",
                                In = ParameterLocation.Header,
                                Example = new OpenApiString("Bearer <token>"),
                                Required = true,
                                Schema = new OpenApiSchema() { Type = "string" },
                            },
                            // Test parameter is not relevant for external users?
                            // new OpenApiParameter()
                            // {
                            //     Name = "test",
                            //     In = ParameterLocation.Query,
                            //     Schema = new OpenApiSchema()
                            //     {
                            //         Type = "boolean"
                            //     }
                            // }
                        },
                        Responses = new OpenApiResponses()
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Description = "Exchanged token",
                                Content =
                                {
                                    ["text/plain"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchema()
                                        {
                                            Type = "string",
                                            Example = new OpenApiString(
                                                "eyJraWQiOiJIdFlaMU1UbFZXUGNCV0JQVWV3TmxZd1RCRklicU1Hb081O"
                                            ),
                                        },
                                    },
                                },
                            },
                            ["401"] = new OpenApiResponse() { Description = "Unauthorized" },
                            ["400"] = new OpenApiResponse() { Description = "Bad Request" },
                            ["429"] = new OpenApiResponse() { Description = "Too Many Requests" },
                        },
                    },
                },
                Servers =
                [
                    new OpenApiServer { Description = "Production environment", Url = "https://platform.altinn.no" },
                    new OpenApiServer { Description = "Test environment", Url = "https://platform.tt02.altinn.no" },
                ],
            }
        );
        document.Paths.Add(
            "http://local.altinn.cloud/Home/GetTestUserToken",
            new OpenApiPathItem()
            {
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation()
                    {
                        Tags = authTags,
                        Summary = "Get a test user token",
                        Description = "Get a test user token for use in the local development environment",
                        Parameters =
                        [
                            new OpenApiParameter()
                            {
                                Name = "userId",
                                Description = "The user id of the test user",
                                In = ParameterLocation.Query,
                                Required = true,
                                Schema = new OpenApiSchema() { Type = "int32", Example = new OpenApiString("1337") },
                            },
                            new OpenApiParameter()
                            {
                                Name = "authenticationLevel",
                                Description = "The authentication level of the test user",
                                In = ParameterLocation.Query,
                                Schema = new OpenApiSchema()
                                {
                                    Type = "int32",
                                    Enum =
                                    [
                                        new OpenApiInteger(0),
                                        new OpenApiInteger(1),
                                        new OpenApiInteger(2),
                                        new OpenApiInteger(3),
                                        new OpenApiInteger(4),
                                        new OpenApiInteger(5),
                                    ],
                                    Default = new OpenApiInteger(3),
                                },
                                Required = true,
                            },
                        ],
                        Responses = new OpenApiResponses()
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Description = "Test user token",
                                Content =
                                {
                                    ["text/plain"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchema()
                                        {
                                            Type = "string",
                                            Example = new OpenApiString(
                                                "eyJraWQiOiJIdFlaMU1UbFZXUGNCV0JQVWV3TmxZd1RCRklicU1Hb081O"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            }
        );
    }

    private void AddRoutsForDataType(OpenApiDocument doc, ApplicationMetadata appMetadata, DataType dataType)
    {
        var tags = new[]
        {
            new OpenApiTag()
            {
                Name = $"{(dataType.AppLogic?.ClassRef is null ? "FileData" : "FormData")} {dataType.Id}",
                Description = $"Operations on data elements of type {dataType.Id}",
            },
        };
        var schema = GetSchemaForDataType(dataType);
        if (schema is not null)
        {
            AddOperationsForFormData(doc, tags, schema, dataType, appMetadata);
        }
        else
        {
            AddRoutsForAttachmentDataType(doc, tags, dataType, appMetadata);
        }
    }

    private static void AddOperationsForFormData(
        OpenApiDocument doc,
        OpenApiTag[] tags,
        OpenApiSchema schema,
        DataType dataType,
        ApplicationMetadata appMetadata
    )
    {
        var jsonType = new OpenApiMediaType() { Schema = schema };
        var xmlType = new OpenApiMediaType()
        {
            Schema = new OpenApiSchema()
            {
                Type = "string",
                Format = "binary",
                Title = "Xml",
                Description = $"""See [xml schema](/{appMetadata.Id}/xmlSchema/{dataType.Id})""",
            },
            Example = new OpenApiString("<xml></xml>"),
        };
        doc.Paths.Add(
            $"/{appMetadata.Id}/instances/{{instanceOwnerPartyId}}/{{instanceGuid}}/data/{{dataGuid}}/{dataType.Id}",
            new OpenApiPathItem()
            {
                Summary = $"Operations for {dataType.Id}",
                Description = $"CRUD operations for data of type {dataType.Id}",
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation()
                    {
                        Tags = tags,
                        Summary = "Get data",
                        Description = $"""
                        Get data for a specific data element

                        see [JSON Schema](/{appMetadata.Id}/api/jsonschema/{dataType.Id})
                        """,
                        Responses = Snippets.AddCommonErrorResponses(
                            HttpStatusCode.OK,
                            new OpenApiResponse()
                            {
                                Description = """
                                # Data found

                                The response body contains the data in the format specified by the Accept header.


                                """,
                                Content = { ["application/json"] = jsonType, ["application/xml"] = xmlType },
                            }
                        ),
                    },
                    [OperationType.Put] = new OpenApiOperation()
                    {
                        Tags = tags,
                        Summary = "Replace data element content",
                        Description = "Update data for a specific data element",
                        RequestBody = new OpenApiRequestBody()
                        {
                            Required = true,
                            Content = { ["application/json"] = jsonType, ["application/xml"] = xmlType },
                        },
                    },
                },
                Parameters =
                [
                    Snippets.InstanceOwnerPartyIdParameterReference,
                    Snippets.InstanceGuidParameterReference,
                    Snippets.DataGuidParameterReference,
                ],
            }
        );
        doc.Paths.Add(
            $"/{appMetadata.Id}/instances/{{instanceOwnerPartyId}}/{{instanceGuid}}/data/{dataType.Id}",
            new OpenApiPathItem()
            {
                Summary = $"Operations for {dataType.Id}",
                Description = $"CRUD operations for data of type {dataType.Id}",
                Operations =
                {
                    [OperationType.Post] = new OpenApiOperation()
                    {
                        Tags = tags,
                        Summary = "Create data",
                        Description = "Create data for a specific data element",
                        RequestBody = new OpenApiRequestBody()
                        {
                            Required = true,
                            Content = { ["application/json"] = jsonType, ["application/xml"] = xmlType },
                        },
                    },
                },
                Parameters =
                [
                    Snippets.InstanceOwnerPartyIdParameterReference,
                    Snippets.InstanceGuidParameterReference,
                    Snippets.DataGuidParameterReference,
                ],
            }
        );
    }

    private static void AddRoutsForAttachmentDataType(
        OpenApiDocument doc,
        OpenApiTag[] tags,
        DataType dataType,
        ApplicationMetadata appMetadata
    )
    {
        doc.Paths.Add(
            $"/{appMetadata.Id}/instances/{{instanceOwnerPartyId}}/{{instanceGuid}}/data/{{dataGuid}}/{dataType.Id}",
            new OpenApiPathItem()
            {
                Summary = $"Operations for {dataType.Id}",
                Description = $"CRUD operations for data of type {dataType.Id}",
                Operations =
                {
                    [OperationType.Get] = new OpenApiOperation()
                    {
                        Tags = tags,
                        Summary = "Get attachment",
                        Description = "Get attachment for a specific data element",
                        Responses = new()
                        {
                            ["200"] = new OpenApiResponse()
                            {
                                Description = "Attachment found",
                                Content =
                                {
                                    ["application/octet-stream"] = new OpenApiMediaType()
                                    {
                                        Schema = new OpenApiSchema() { Format = "binary" },
                                    },
                                },
                            },
                            ["404"] = new OpenApiResponse() { Description = "Attachment not found" },
                        },
                    },
                },
                Parameters =
                [
                    Snippets.InstanceOwnerPartyIdParameterReference,
                    Snippets.InstanceGuidParameterReference,
                    Snippets.DataGuidParameterReference,
                ],
            }
        );
        doc.Paths.Add(
            $"/{appMetadata.Id}/instances/{{instanceOwnerPartyId}}/{{instanceGuid}}/data/{dataType.Id}",
            new OpenApiPathItem()
            {
                Summary = $"Operations for {dataType.Id}",
                Description = $"CRUD operations for data of type {dataType.Id}",
                Operations =
                {
                    [OperationType.Post] = new OpenApiOperation()
                    {
                        Tags = tags,
                        Summary = "Create attachment",
                        RequestBody = new OpenApiRequestBody()
                        {
                            Required = true,
                            Content =
                                dataType.AllowedContentTypes?.ToDictionary(
                                    contentType => contentType,
                                    contentType => new OpenApiMediaType()
                                )
                                ?? new Dictionary<string, OpenApiMediaType>()
                                {
                                    ["application/octet-stream"] = new OpenApiMediaType(),
                                },
                        },
                        Responses = new() { ["201"] = new OpenApiResponse() { Description = "Attachment created" } },
                    },
                },
                Parameters =
                [
                    Snippets.InstanceOwnerPartyIdParameterReference,
                    Snippets.InstanceGuidParameterReference,
                    Snippets.DataGuidParameterReference,
                ],
            }
        );
    }

    private OpenApiSchema? GetSchemaForDataType(DataType dataType)
    {
        var classRef = dataType.AppLogic?.ClassRef;
        if (classRef == null)
        {
            return null;
        }
        var model = _appModel.GetModelType(classRef);
        if (model == null)
        {
            return null;
        }
        var schema = _schemaGenerator.GenerateSchema(model, _schemaRepository);
        schema.Title = dataType.Id;
        schema.Description =
            dataType.Description?.GetValueOrDefault("en")
            ?? dataType.Description?.GetValueOrDefault("nb")
            ?? dataType.Description?.FirstOrDefault().Value;
        return schema;
    }
}

/// <summary>
/// Common parts from the schema generator
/// </summary>
public static class Snippets
{
    /// <summary>
    /// Schema for the POST endpoint for creating a new instance
    /// </summary>
    public static OpenApiSchema InstanceWriteSchema =>
        new()
        {
            Title = "InstanceWrite",
            Properties =
            {
                ["instanceOwner"] = new OpenApiSchema()
                {
                    Type = "object",
                    Title = "Altnernate ways to spcecify the instance owner",
                    Description = "Only one of these should be spcecified when creating a new inistance",
                    Properties =
                    {
                        ["partyId"] = new OpenApiSchema()
                        {
                            Type = "string",
                            Nullable = true,
                            Format = "int32",
                        },
                        ["personNumber"] = new OpenApiSchema()
                        {
                            Type = "string",
                            Nullable = true,
                            Pattern = @"^\d{11}$",
                        },
                        ["organisationNumber"] = new OpenApiSchema()
                        {
                            Type = "string",
                            Nullable = true,
                            Pattern = @"^\d{9}$",
                        },
                        ["username"] = new OpenApiSchema()
                        {
                            Type = "string",
                            Nullable = true,
                            Description =
                                "Initialization based on username is not yet supported (https://github.com/Altinn/app-lib-dotnet/issues/652)",
                        },
                    },
                },
                ["dueBefore"] = new OpenApiSchema() { Type = "string", Format = "date-time" },
                ["visibleAfter"] = new OpenApiSchema() { Type = "string", Format = "date-time" },
            },
        };

    /// <summary>
    /// Schema for patching multiple data elements at once
    /// </summary>
    public static OpenApiSchema PatchSchema =>
        new()
        {
            Title = "Run patches on multiple Form data elements at once",
            Type = "object",
            Properties =
            {
                ["patches"] = new()
                {
                    Type = "array",
                    Items = new()
                    {
                        Type = "object",
                        Properties =
                        {
                            ["dataElementId"] = new() { Type = "string", Format = "guid" },
                            ["patch"] = new()
                            {
                                Type = "object",
                                Title = "Json patch",
                                Description = "A standard RFC 6902 document describing changes to one data element",
                                Properties =
                                {
                                    ["operations"] = new()
                                    {
                                        Type = "array",
                                        Items = new()
                                        {
                                            Type = "object",
                                            Required = new HashSet<string>() { "op", "path" },
                                            Properties =
                                            {
                                                ["op"] = new()
                                                {
                                                    Title = "Patch operation",
                                                    Type = "string",
                                                    Enum =
                                                    [
                                                        new OpenApiString("add"),
                                                        new OpenApiString("remove"),
                                                        new OpenApiString("replace"),
                                                        new OpenApiString("move"),
                                                        new OpenApiString("copy"),
                                                        new OpenApiString("test"),
                                                    ],
                                                },
                                                ["from"] = new() { Title = "JsonPointer", Type = "string" },
                                                ["path"] = new() { Title = "JsonPointer" },
                                                ["value"] = new() { Type = "any" },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                ["ignoredValidators"] = new()
                {
                    Title = "List of validators to not run incrementally",
                    Description =
                        "This is used for saving server resources, when frontend has a duplicated version of the validator. The validators will be executed on process/next anyway",
                    Items = new() { Type = "string" },
                },
            },
        };

    /// <summary>
    /// Reference to the shared instance owner party id parameter
    /// </summary>
    public static OpenApiParameter InstanceOwnerPartyIdParameterReference =>
        new()
        {
            Reference = new OpenApiReference() { Id = "instanceOwnerPartyId", Type = ReferenceType.Parameter },
        };

    /// <summary>
    /// Reference to the shared instance guid parameter
    /// </summary>
    public static OpenApiParameter InstanceGuidParameterReference =>
        new()
        {
            Reference = new OpenApiReference() { Id = "instanceGuid", Type = ReferenceType.Parameter },
        };

    /// <summary>
    /// Reference to the shared data guid parameter
    /// </summary>
    public static OpenApiParameter DataGuidParameterReference =>
        new()
        {
            Reference = new OpenApiReference() { Id = "dataGuid", Type = ReferenceType.Parameter },
        };

    /// <summary>
    /// Reference to the shared language parameter
    /// </summary>
    public static OpenApiParameter LanguageParameterReference =>
        new()
        {
            Reference = new OpenApiReference() { Id = "language", Type = ReferenceType.Parameter },
        };

    /// <summary>
    /// q
    /// </summary>
    public static IDictionary<string, OpenApiParameter> ComponentParameters =>
        new Dictionary<string, OpenApiParameter>
        {
            [InstanceOwnerPartyIdParameterReference.Reference.Id] = new()
            {
                Name = "instanceOwnerPartyId",
                Description =
                    "PartyId for the owner of the instance, this is altinns internal id for the organisation, person or self registered user. Might be the current user, or ",
                In = ParameterLocation.Path,
                Required = true,
                Schema = new OpenApiSchema() { Type = "integer" },
            },
            [InstanceGuidParameterReference.Reference.Id] = new()
            {
                Name = "instanceGuid",
                Description = "The guid part of instance.Id",
                In = ParameterLocation.Path,
                Required = true,
                Schema = new OpenApiSchema() { Type = "guid" },
            },
            ["dataGuid"] = new()
            {
                Name = "dataGuid",
                Description = "Id of this data element that belongs to an instance",
                In = ParameterLocation.Path,
                Required = true,
                Schema = new OpenApiSchema() { Type = "guid" },
            },
            ["language"] = new()
            {
                Name = "language",
                In = ParameterLocation.Query,
                AllowEmptyValue = false,
                Example = new OpenApiString("nb"),
                Description =
                    "Some apps make changes to the data models or validation based on the active language of the user",
                Required = false,
                Schema = new OpenApiSchema() { Type = "string", Pattern = @"\w\w" },
            },
        };

    /// <summary>
    /// Schema for problem details
    /// </summary>
    public static OpenApiResponse ProblemDetailsResponseSchema =>
        new OpenApiResponse()
        {
            Description = "Problem details",
            Content =
            {
                ["application/problem+json"] = new OpenApiMediaType()
                {
                    Schema = new()
                    {
                        Type = "object",
                        Properties =
                        {
                            ["type"] = new OpenApiSchema()
                            {
                                Type = "string",
                                Nullable = true,
                                Example = new OpenApiString("https://datatracker.ietf.org/doc/html/rfc6902/"),
                            },
                            ["title"] = new OpenApiSchema()
                            {
                                Type = "string",
                                Nullable = true,
                                Example = new OpenApiString("Error in data processing"),
                            },
                            ["status"] = new OpenApiSchema()
                            {
                                Type = "integer",
                                Format = "int32",
                                Nullable = true,
                                Example = new OpenApiInteger(400),
                            },
                            ["detail"] = new OpenApiSchema()
                            {
                                Type = "string",
                                Nullable = true,
                                Example = new OpenApiString("Actually usefull description of the error"),
                            },
                            ["instance"] = new OpenApiSchema() { Type = "string", Nullable = true },
                        },
                    },
                },
            },
        };

    /// <summary>
    /// Security scheme for Altinn token
    /// </summary>
    public static readonly OpenApiSecurityScheme AltinnTokenSecurityScheme = new OpenApiSecurityScheme()
    {
        Reference = new OpenApiReference() { Id = "AltinnToken", Type = ReferenceType.SecurityScheme },
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = """
            Get a token for [localtest](http://local.altinn.cloud/Home/Tokens)
            or by exchanging a maskinporten token with the [token exchange endpoint](https://docs.altinn.studio/api/authentication/spec/#/Authentication/get_exchange__tokenProvider_)
            """,
    };

    /// <summary>
    /// Reference to the ProblemDetails common response
    /// </summary>
    public static readonly OpenApiReference ProblemDetailsResponseReference = new()
    {
        Id = "ProblemDetails",
        Type = ReferenceType.Response,
    };

    /// <summary>
    /// Add common error responses to a response collection
    /// </summary>
    public static OpenApiResponses AddCommonErrorResponses(HttpStatusCode statusCode, OpenApiResponse response)
    {
        var responses = new OpenApiResponses()
        {
            [((int)statusCode).ToString(CultureInfo.InvariantCulture)] = response,
        };
        return AddCommonErrorResponses(responses);
    }

    /// <summary>
    /// Add common error responses to a response collection
    /// </summary>
    public static OpenApiResponses AddCommonErrorResponses(OpenApiResponses responses)
    {
        responses.TryAdd(
            "400",
            new OpenApiResponse() { Description = "Bad request", Reference = ProblemDetailsResponseReference }
        );
        responses.TryAdd(
            "401",
            new OpenApiResponse() { Description = "Unauthorized", Reference = ProblemDetailsResponseReference }
        );
        responses.TryAdd(
            "403",
            new OpenApiResponse() { Description = "Forbidden", Reference = ProblemDetailsResponseReference }
        );
        responses.TryAdd(
            "404",
            new OpenApiResponse() { Description = "Not found", Reference = ProblemDetailsResponseReference }
        );
        responses.TryAdd(
            "500",
            new OpenApiResponse() { Description = "Internal server error", Reference = ProblemDetailsResponseReference }
        );
        return responses;
    }
}

/// <summary>
/// Visitor that modifies the schema after it has been generated
/// </summary>
public class SchemaPostVisitor : OpenApiVisitorBase
{
    /// <inheritdoc />
    public override void Visit(OpenApiSchema schema)
    {
        // Remove `altinnRowId` from the data element schema (they are not intended for external usage)
        schema.Properties.Remove("altinnRowId");

        // openapi has xml extensions, but they can't represent tags with both attributes and values
        // <tag orid="323">value</tag>, so we just zero properties from SwaggerGen
        schema.Xml = null;
    }
}

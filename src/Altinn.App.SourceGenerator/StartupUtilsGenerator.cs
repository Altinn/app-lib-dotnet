using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Altinn.App.SourceGenerator;

[Generator]
public class StartupUtilsGenerator: ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        string source = @"// <auto-generated />
using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Altinn.App.Generated.Startup;

public static class StartupUtils
{
    public static void IncludeXmlComments(SwaggerGenOptions options)
    {
        try
        {
            string fileName = $""{Assembly.GetExecutingAssembly().GetName().Name}.xml"";
        string fullFilePath = Path.Combine(AppContext.BaseDirectory, fileName);
        options.IncludeXmlComments(fullFilePath);
        string fullFilePathApi = Path.Combine(AppContext.BaseDirectory, ""Altinn.App.Api.xml"");
        options.IncludeXmlComments(fullFilePathApi);
        }
        catch
        {
            // Swagger will not have the xml-documentation to describe the api's.
        }
    }

    public static string GetApplicationId()
    {
        string appMetaDataString = File.ReadAllText(""config/applicationmetadata.json"");
        JObject appMetadataJObject = JObject.Parse(appMetaDataString);
        return appMetadataJObject.SelectToken(""id"").Value<string>();
    }
}
";
        context.AddSource("StartupUtils.g.cs", SourceText.From(source, Encoding.UTF8));
    }
}
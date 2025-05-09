using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;
using Altinn.App.Analyzers.IncrementalGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Altinn.App.SourceGenerator.Tests;

public class DiagnosticTests
{
    private const string Source = """
        #nullable enable
        using System;
        using System.Collections.Generic;
        using System.Text.Json.Serialization;

        namespace Altinn.App.SourceGenerator.Tests;

        public class Skjema
        {
            [JsonPropertyName("skjemanummer")]
            public string? Skjemanummer { get; set; }

            [JsonPropertyName("skjemaversjon")]
            public string? Skjemaversjon { get; set; }

            [JsonPropertyName("skjemainnhold")]
            public List<SkjemaInnhold?>? Skjemainnhold { get; set; }
        }

        public class SkjemaInnhold
        {
            [JsonPropertyName("altinnRowId")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public Guid AltinnRowId { get; set; }

            [JsonPropertyName("navn")]
            public string? Navn { get; set; }

            [JsonPropertyName("alder")]
            public int? Alder { get; set; }

            [JsonPropertyName("deltar")]
            public bool? Deltar { get; set; }
        }

        """;

    [Fact]
    public void RunNoDiagnostic()
    {
        var applicationMetadata = """
            {
                "$schema": "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/application/application-metadata.schema.v1.json",
                "id": "ttd/source-generator-test",
                "title": {},
                "org": "ttd",
                "partyTypesAllowed": {},
                "dataTypes": [{
                    "id": "form",
                    "appLogic": {
                        "classRef": "Altinn.App.SourceGenerator.Tests.Skjema"
                    }
                }]
            }

            """;

        var diagnostics = RunFormDataWrapper([Source], applicationMetadata);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ClassNotFound()
    {
        var applicationMetadata = """
            {
                "$schema": "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/application/application-metadata.schema.v1.json",
                "id": "ttd/source-generator-test",
                "title": {},
                "org": "ttd",
                "partyTypesAllowed": {},
                "dataTypes": [{
                    "id": "form",
                    "appLogic": {
                        "classRef": "Altinn.App.SourceGenerator.Tests.NotFound"
                    }
                }]
            }

            """;

        var diagnostics = RunFormDataWrapper([Source], applicationMetadata);
        await Verify(diagnostics);
    }

    [Fact]
    public async Task RunJsonError()
    {
        var applicationMetadata = """
            {
                "title": {,},
            }

            """;

        var diagnostics = RunFormDataWrapper([Source], applicationMetadata);
        await Verify(diagnostics);
    }

    [Fact]
    public void RunJsonNoDataTypes()
    {
        var applicationMetadata = """
            {
                "$schema": "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/application/application-metadata.schema.v1.json",
                "id": "ttd/source-generator-test",
            }

            """;

        var diagnostics = RunFormDataWrapper([Source], applicationMetadata);
        Assert.Empty(diagnostics);
    }

    static ImmutableArray<Diagnostic> RunFormDataWrapper(string[] syntax, string applicationMetadata)
    {
        var currentAssembly = Assembly.GetAssembly(typeof(Skjema));
        // Get references so that the test compilation can reference system libraries
        IEnumerable<PortableExecutableReference> references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Where(assembly => assembly != currentAssembly)
            .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat([MetadataReference.CreateFromFile(typeof(JsonPropertyNameAttribute).Assembly.Location)]);

        var sources = syntax
            .Select(s => CSharpSyntaxTree.ParseText(s, path: "/Altinn/ttd/altinn-app-frontend/models/Models.cs"))
            .ToArray();
        var compilation = CSharpCompilation.Create(
            "name",
            sources,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        var generator = new FormDataWrapperGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts(
            [new AdditionalTextImplementation(applicationMetadata, "C:\\temp\\config\\applicationmetadata.json")]
        );
        var results = driver.RunGenerators(compilation);

        var runResult = results.GetRunResult();

        return runResult.Diagnostics;
    }
}

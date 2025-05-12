using Altinn.App.Analyzers.SourceTextGenerator;
using Altinn.App.Analyzers.Utils;
using NanoJsonReader;

namespace Altinn.App.Analyzers.IncrementalGenerator;

/// <summary>
/// Generate IFormDataWrapper implementations for classes in models/*.cs in the app.
/// </summary>
[Generator]
public class FormDataWrapperGenerator : IIncrementalGenerator
{
    private sealed record Result<T>(T? Value, EquatableArray<EquatableDiagnostic> Diagnostics)
        where T : class
    {
        public Result(EquatableDiagnostic diagnostics)
            : this(null, new EquatableArray<EquatableDiagnostic>([diagnostics])) { }

        public Result(T value)
            : this(value, EquatableArray<EquatableDiagnostic>.Empty) { }
    };

    private sealed record ModelClassOrDiagnostic(
        string? ClassName,
        Location? Location,
        EquatableArray<EquatableDiagnostic> Diagnostics
    )
    {
        public ModelClassOrDiagnostic(EquatableDiagnostic diagnostic)
            : this(null, null, new([diagnostic])) { }

        public ModelClassOrDiagnostic(string className, Location? location)
            : this(className, location, EquatableArray<EquatableDiagnostic>.Empty) { }
    };

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootClasses = context
            .AdditionalTextsProvider.Where(text =>
                text.Path.Replace('\\', '/').EndsWith("config/applicationmetadata.json")
            )
            .SelectMany(ParseModelClassOrDiagnostic);

        var modelPathNodesProvider = rootClasses.Combine(context.CompilationProvider).Select(CreateNodeThree);

        context.RegisterSourceOutput(modelPathNodesProvider, GenerateFromNode);
    }

    private static ImmutableArray<ModelClassOrDiagnostic> ParseModelClassOrDiagnostic(
        AdditionalText text,
        CancellationToken token
    )
    {
        try
        {
            var textContent = text.GetText(token)?.ToString();
            if (textContent is null)
            {
                return
                [
                    new(
                        new EquatableDiagnostic(
                            Diagnostics.FormDataWrapperGenerator.AppMetadataError,
                            FileLocationHelper.GetLocation(text, 0, null),
                            ["Failed to read applicationmetadata.json"]
                        )
                    ),
                ];
            }

            var appMetadata = JsonValue.Parse(textContent);
            if (appMetadata.Type != JsonType.Object)
            {
                return
                [
                    new(
                        new EquatableDiagnostic(
                            Diagnostics.FormDataWrapperGenerator.AppMetadataError,
                            FileLocationHelper.GetLocation(text, appMetadata.Start, appMetadata.End),
                            ["applicationmetadata.json is not a valid JSON object"]
                        )
                    ),
                ];
            }

            var dataTypes = appMetadata.GetProperty("dataTypes");
            if (dataTypes?.Type != JsonType.Array)
            {
                return
                [
                    new(
                        new(
                            Diagnostics.FormDataWrapperGenerator.AppMetadataError,
                            FileLocationHelper.GetLocation(text, appMetadata.Start, appMetadata.End),
                            ["the property dataTypes is not a valid JSON array"]
                        )
                    ),
                ];
            }

            List<ModelClassOrDiagnostic> rootClasses = [];
            foreach (var dataType in dataTypes.GetArrayValues())
            {
                if (dataType.Type != JsonType.Object)
                {
                    continue;
                }

                var appLogic = dataType.GetProperty("appLogic");
                if (appLogic?.Type != JsonType.Object)
                {
                    continue;
                }

                var classRef = appLogic.GetProperty("classRef");
                if (classRef?.Type != JsonType.String)
                {
                    continue;
                }

                rootClasses.Add(
                    new(classRef.GetString(), FileLocationHelper.GetLocation(text, classRef.Start, classRef.End))
                );
            }

            return [.. rootClasses];
        }
        catch (NanoJsonException e)
        {
            return
            [
                new(
                    new EquatableDiagnostic(
                        Diagnostics.FormDataWrapperGenerator.AppMetadataError,
                        FileLocationHelper.GetLocation(text, e.StartIndex, e.EndIndex),
                        [e.Message]
                    )
                ),
            ];
        }
    }

    private static Result<ModelPathNode> CreateNodeThree(
        (ModelClassOrDiagnostic, Compilation) tuple,
        CancellationToken _
    )
    {
        var (rootSymbolFullName, compilation) = tuple;
        if (rootSymbolFullName.ClassName is null)
        {
            return new Result<ModelPathNode>(null, rootSymbolFullName.Diagnostics);
        }
        var rootSymbol = compilation.GetBestTypeByMetadataName(rootSymbolFullName.ClassName);
        if (rootSymbol == null)
        {
            return new Result<ModelPathNode>(
                new EquatableDiagnostic(
                    Diagnostics.FormDataWrapperGenerator.AppMetadataError,
                    rootSymbolFullName.Location,
                    [$"Could not find class {rootSymbolFullName.ClassName} in the compilation"]
                )
            );
        }

        return new Result<ModelPathNode>(
            new ModelPathNode("", "", SourceReaderUtils.TypeSymbolToString(rootSymbol), GetNodeProperties(rootSymbol))
        );
    }

    private static EquatableArray<ModelPathNode>? GetNodeProperties(INamedTypeSymbol namedTypeSymbol)
    {
        var nodeProperties = new List<ModelPathNode>();
        foreach (var property in namedTypeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (
                property.IsStatic
                || property.IsReadOnly
                || property.IsWriteOnly
                || property.IsImplicitlyDeclared
                || property.IsIndexer
            )
            {
                // Skip static, readonly, writeonly, implicitly declared and indexer properties
                continue;
            }
            var (propertyTypeSymbol, propertyCollectionTypeSymbol) = SourceReaderUtils.GetTypeFromProperty(
                property.Type
            );

            var cSharpName = property.Name;
            var jsonName = SourceReaderUtils.GetJsonName(property) ?? cSharpName;
            var typeString = SourceReaderUtils.TypeSymbolToString(propertyTypeSymbol);
            var collectionTypeString = propertyCollectionTypeSymbol is null
                ? null
                : SourceReaderUtils.TypeSymbolToString(propertyCollectionTypeSymbol);

            if (
                propertyTypeSymbol is INamedTypeSymbol propertyNamedTypeSymbol
                && !propertyNamedTypeSymbol.ContainingNamespace.ToString().StartsWith("System")
            )
            {
                nodeProperties.Add(
                    new ModelPathNode(
                        cSharpName,
                        jsonName,
                        typeString,
                        GetNodeProperties(propertyNamedTypeSymbol),
                        collectionTypeString
                    )
                );
            }
            else
            {
                nodeProperties.Add(new ModelPathNode(cSharpName, jsonName, typeString, null, collectionTypeString));
            }
        }
        return nodeProperties;
    }

    private void GenerateFromNode(SourceProductionContext context, Result<ModelPathNode> result)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.CreateDiagnostic());
        }
        if (result is not { Value: { } node })
            return;
        var sourceText = SourceTextGenerator.SourceTextGenerator.GenerateSourceText(node, "public");
        context.AddSource(node.Name + "FormDataWrapper.g.cs", sourceText);
    }
}

// using System.Diagnostics.CodeAnalysis;
// using Altinn.App.Analyzers.ApplicationMetadata;
// using Altinn.App.Analyzers.Json;
// using Altinn.App.Analyzers.Layouts;
// using Microsoft.CodeAnalysis.Text;

// namespace Altinn.App.Analyzers;

// internal readonly record struct MetadataAnalyzerContext
// {
//     private readonly Action<Diagnostic> _reportDiagnostic;
//     private readonly MetadataAnalyzer _analyzer;

//     public CancellationToken CancellationToken { get; }

//     public string ProjectDir { get; }

//     public Compilation Compilation { get; }

//     public AdditionalText? AdditionalFile { get; }

//     public ImmutableArray<AdditionalText> AdditionalFiles { get; }

//     public Action? OnCompilationBefore => _analyzer.OnCompilationBefore;
//     public Action? OnApplicationMetadataReadBefore => _analyzer.OnApplicationMetadataReadBefore;
//     public Action? OnApplicationMetadataDeserializationBefore => _analyzer.OnApplicationMetadataDeserializationBefore;
//     public Action? OnLayoutSetsReadBefore => _analyzer.OnLayoutSetsReadBefore;
//     public Action? OnLayoutSetsDeserializationBefore => _analyzer.OnLayoutSetsDeserializationBefore;

//     internal MetadataAnalyzerContext(
//         CompilationAnalysisContext compilationAnalysisContext,
//         string projectDir,
//         MetadataAnalyzer analyzer
//     )
//     {
//         _reportDiagnostic = compilationAnalysisContext.ReportDiagnostic;
//         ProjectDir = projectDir;
//         Compilation = compilationAnalysisContext.Compilation;
//         CancellationToken = compilationAnalysisContext.CancellationToken;
//         AdditionalFiles = compilationAnalysisContext.Options.AdditionalFiles;
//         _analyzer = analyzer;
//     }

//     internal MetadataAnalyzerContext(
//         AdditionalFileAnalysisContext additionalFileAnalysisContext,
//         AdditionalText additionalFile,
//         string projectDir,
//         MetadataAnalyzer analyzer
//     )
//     {
//         _reportDiagnostic = additionalFileAnalysisContext.ReportDiagnostic;
//         AdditionalFile = additionalFile;
//         AdditionalFiles = additionalFileAnalysisContext.Options.AdditionalFiles;
//         ProjectDir = projectDir;
//         Compilation = additionalFileAnalysisContext.Compilation;
//         CancellationToken = additionalFileAnalysisContext.CancellationToken;
//         _analyzer = analyzer;
//     }

//     public void ReportDiagnostic(Diagnostic diagnostic) => _reportDiagnostic(diagnostic);

//     public AdditionalText? GetAdditionlFileEndingWithPath(string path)
//     {
//         if (AdditionalFile is not null && AdditionalFile.Path.EndsWith(path, StringComparison.Ordinal))
//             return AdditionalFile;

//         return AdditionalFiles.FirstOrDefault(f => f.Path.EndsWith(path, StringComparison.Ordinal));
//     }
// }

// [DiagnosticAnalyzer(LanguageNames.CSharp)]
// public sealed class MetadataAnalyzer : DiagnosticAnalyzer
// {
//     public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

//     public Action? OnCompilationBefore { get; set; }
//     public Action? OnApplicationMetadataReadBefore { get; set; }
//     public Action? OnApplicationMetadataDeserializationBefore { get; set; }
//     public Action? OnLayoutSetsReadBefore { get; set; }
//     public Action? OnLayoutSetsDeserializationBefore { get; set; }

//     public override void Initialize(AnalysisContext context)
//     {
//         var configFlags = GeneratedCodeAnalysisFlags.None;
//         context.ConfigureGeneratedCodeAnalysis(configFlags);
//         context.EnableConcurrentExecution();

//         context.RegisterCompilationAction(OnCompilation);
//         context.RegisterAdditionalFileAction(OnAdditionalFileAction);
//     }

//     private static bool TryGetProjectDir(
//         AnalyzerConfigOptionsProvider optionsProvider,
//         [NotNullWhen(true)] out string? projectDir
//     )
//     {
//         var globalOptions = optionsProvider.GlobalOptions;
//         return globalOptions.TryGetValue("build_property.projectdir", out projectDir);
//     }

//     private static readonly string[] _interestingFiles =
//     [
//         ApplicationMetadataFileReader.RelativeFilePath,
//         LayoutSetsFileReader.RelativeFilePath,
//     ];

//     private void OnAdditionalFileAction(AdditionalFileAnalysisContext additionalFileAnalysisContext)
//     {
//         try
//         {
//             var file = additionalFileAnalysisContext.AdditionalFile;
//             if (Array.Exists(_interestingFiles, f => file.Path.EndsWith(f, StringComparison.Ordinal)))
//             {
//                 var optionsProvider = additionalFileAnalysisContext.Options.AnalyzerConfigOptionsProvider;
//                 if (!TryGetProjectDir(optionsProvider, out var projectDir))
//                 {
//                     additionalFileAnalysisContext.ReportDiagnostic(
//                         Diagnostic.Create(Diagnostics.ProjectNotFound, Location.None)
//                     );
//                     return;
//                 }

//                 var context = new MetadataAnalyzerContext(additionalFileAnalysisContext, file, projectDir, this);

//                 AnalyzeApplicationMetadata(in context);
//             }
//         }
//         catch (Exception ex)
//         {
//             if (ex is OperationCanceledException)
//                 return;
//             additionalFileAnalysisContext.ReportDiagnostic(
//                 Diagnostic.Create(Diagnostics.UnknownError, Location.None, ex.Message, ex.StackTrace)
//             );
//         }
//     }

//     private void OnCompilation(CompilationAnalysisContext compilationAnalysisContext)
//     {
//         if (compilationAnalysisContext.Options.AdditionalFiles.Length != 0)
//             return;
//         try
//         {
//             OnCompilationBefore?.Invoke();

//             var optionsProvider = compilationAnalysisContext.Options.AnalyzerConfigOptionsProvider;
//             if (!TryGetProjectDir(optionsProvider, out var projectDir))
//             {
//                 compilationAnalysisContext.ReportDiagnostic(
//                     Diagnostic.Create(Diagnostics.ProjectNotFound, Location.None)
//                 );
//                 return;
//             }

//             var context = new MetadataAnalyzerContext(compilationAnalysisContext, projectDir, this);

//             AnalyzeApplicationMetadata(in context);
//         }
//         catch (Exception ex)
//         {
//             if (ex is OperationCanceledException)
//                 return;
//             compilationAnalysisContext.ReportDiagnostic(
//                 Diagnostic.Create(Diagnostics.UnknownError, Location.None, ex.Message, ex.StackTrace)
//             );
//         }
//     }

//     private static void AnalyzeApplicationMetadata(in MetadataAnalyzerContext context)
//     {
//         var metadataResult = ApplicationMetadataFileReader.Read(in context);
//         switch (metadataResult)
//         {
//             case ApplicationMetadataResult.Content result:
//                 AnalyzeApplicationMetadataContent(in context, result);
//                 break;
//             case ApplicationMetadataResult.FileNotFound result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(Diagnostics.ApplicationMetadata.FileNotFound, Location.None, result.FilePath)
//                 );
//                 break;
//             case ApplicationMetadataResult.CouldNotReadFile result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.ApplicationMetadata.FileNotReadable,
//                         GetLocation(result.FilePath),
//                         result.Exception.Message,
//                         result.Exception.StackTrace
//                     )
//                 );
//                 break;
//             case ApplicationMetadataResult.CouldNotParse result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.ApplicationMetadata.ParsingFailure,
//                         GetLocation(result.FilePath, result.SourceText),
//                         result.Exception.Message,
//                         result.Exception.StackTrace
//                     )
//                 );
//                 break;
//             case ApplicationMetadataResult.CouldNotParseField result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.ApplicationMetadata.ParsingFailure,
//                         GetLocation(result.FilePath, result.Token, result.SourceText),
//                         result.Token.PropertyName,
//                         "field"
//                     )
//                 );
//                 break;
//             case ApplicationMetadataResult.Cancelled:
//                 break;
//         }
//     }

//     private static void AnalyzeApplicationMetadataContent(
//         in MetadataAnalyzerContext context,
//         ApplicationMetadataResult.Content content
//     )
//     {
//         if (context.CancellationToken.IsCancellationRequested)
//             return;

//         var (metadata, sourceText, filePath) = content;

//         foreach (var dataType in metadata.DataTypes.Value)
//         {
//             if (dataType.AppLogic.Value is not { } appLogic)
//                 continue;

//             var classRef = appLogic.ClassRef;

//             var classRefSymbol = context.Compilation.GetTypeByMetadataName(classRef.Value);

//             if (classRefSymbol is null)
//             {
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.ApplicationMetadata.DataTypeClassRefInvalid,
//                         GetLocation(filePath, classRef, sourceText),
//                         classRef.Value,
//                         dataType.Id.Value
//                     )
//                 );
//             }
//         }

//         AnalyzeLayoutMetadata(in context, content);
//     }

//     private static void AnalyzeLayoutMetadata(
//         in MetadataAnalyzerContext context,
//         ApplicationMetadataResult.Content appMetadataContent
//     )
//     {
//         if (context.CancellationToken.IsCancellationRequested)
//             return;

//         var layoutsSetResult = LayoutSetsFileReader.Read(in context);

//         switch (layoutsSetResult)
//         {
//             case LayoutSetsResult.Content layoutSetsContent:
//                 AnalyzeLayoutSetsContent(in context, appMetadataContent, layoutSetsContent);
//                 break;
//             case LayoutSetsResult.FileNotFound result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(Diagnostics.Layouts.FileNotFound, Location.None, result.FilePath)
//                 );
//                 break;
//             case LayoutSetsResult.CouldNotReadFile result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.Layouts.FileNotReadable,
//                         GetLocation(result.FilePath),
//                         result.Exception.Message,
//                         result.Exception.StackTrace
//                     )
//                 );
//                 break;
//             case LayoutSetsResult.CouldNotParse result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.Layouts.ParsingFailure,
//                         GetLocation(result.FilePath, result.SourceText),
//                         result.Exception.Message,
//                         result.Exception.StackTrace
//                     )
//                 );
//                 break;
//             case LayoutSetsResult.CouldNotParseField result:
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.Layouts.ParsingFailure,
//                         GetLocation(result.FilePath, result.Token, result.SourceText),
//                         result.Token.PropertyName,
//                         "field"
//                     )
//                 );
//                 break;
//             case LayoutSetsResult.Cancelled:
//                 break;
//         }
//     }

//     private static void AnalyzeLayoutSetsContent(
//         in MetadataAnalyzerContext context,
//         ApplicationMetadataResult.Content appMetadataContent,
//         LayoutSetsResult.Content layoutSetsContent
//     )
//     {
//         var layoutSets = layoutSetsContent.Value.Sets.Value;

//         var onEntry = appMetadataContent.Value.OnEntry.Value;
//         if (onEntry is not null)
//         {
//             var showLayout = onEntry.Show;

//             LayoutSetInfo? foundLayoutSet = null;
//             foreach (var layoutSet in layoutSets)
//             {
//                 if (layoutSet.Id.Value.Equals(showLayout.Value, StringComparison.Ordinal))
//                 {
//                     foundLayoutSet = layoutSet;
//                 }
//             }
//             if (foundLayoutSet is null)
//             {
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.ApplicationMetadata.OnEntryShowRefInvalid,
//                         GetLocation(appMetadataContent.FilePath, showLayout, appMetadataContent.SourceText),
//                         showLayout.Value
//                     )
//                 );
//             }
//         }

//         foreach (var layoutSet in layoutSets)
//         {
//             var dataType = layoutSet.DataType.Value;

//             DataTypeInfo? foundDataTypeInfo = null;
//             foreach (var dataTypeMetadata in appMetadataContent.Value.DataTypes.Value)
//             {
//                 if (dataTypeMetadata.Id.Value.Equals(dataType, StringComparison.Ordinal))
//                 {
//                     foundDataTypeInfo = dataTypeMetadata;
//                 }
//             }

//             if (foundDataTypeInfo is null)
//             {
//                 context.ReportDiagnostic(
//                     Diagnostic.Create(
//                         Diagnostics.Layouts.DataTypeRefInvalid,
//                         GetLocation(layoutSetsContent.FilePath, layoutSet.DataType, layoutSetsContent.SourceText),
//                         dataType,
//                         layoutSet.Id.Value
//                     )
//                 );
//             }
//         }
//     }

//     private static Location GetLocation(string file)
//     {
//         return Location.Create(
//             file,
//             TextSpan.FromBounds(1, 2),
//             new LinePositionSpan(LinePosition.Zero, LinePosition.Zero)
//         );
//     }

//     private static Location GetLocation(string file, SourceText sourceText)
//     {
//         var lines = sourceText.Lines;
//         var textSpan = TextSpan.FromBounds(0, lines[lines.Count - 1].End);
//         return Location.Create(file, textSpan, lines.GetLinePositionSpan(textSpan));
//     }

//     private static Location GetLocation(string file, JsonTokenDescriptor token, SourceText sourceText)
//     {
//         if (token.LineNumber is null || token.LinePosition is null)
//             return Location.None;

//         var lines = sourceText.Lines;
//         var line = lines[token.LineNumber.Value - 1];
//         var textSpan = TextSpan.FromBounds(
//             line.Start + token.LinePosition.Value - 1,
//             line.Start + token.LinePosition.Value - 1
//         );

//         var lineSpan = lines.GetLinePositionSpan(textSpan);
//         return Location.Create(file, textSpan, lineSpan);
//     }
// }

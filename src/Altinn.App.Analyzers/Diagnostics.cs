using System.Reflection;

namespace Altinn.App.Analyzers;

internal static class Diagnostics
{
    internal static readonly DiagnosticDescriptor UnknownError = Error(
        "ALTINN999",
        "Unknown analyzer error",
        "Unknown error occurred during analysis: '{0}' {1}"
    );

    internal static readonly DiagnosticDescriptor ProjectNotFoundError = Error(
        "ALTINN001",
        "Altinn app project not found",
        "While starting analysis, we couldn't find the project directory - contact support"
    );

    internal static readonly DiagnosticDescriptor ApplicationMetadataFileNotFoundError = Error(
        "ALTINN002",
        "Altinn app metadata file not found",
        "Could not find application metadata file at 'config/applicationmetadata.json'"
    );

    internal static readonly DiagnosticDescriptor FailedToParseApplicationMetadataError = Error(
        "ALTINN003",
        "Altinn app metadata file couldn't be parsed",
        "Could not parse application metadata file at 'config/applicationmetadata.json': '{0}' {1}"
    );

    internal static readonly DiagnosticDescriptor DataTypeClassRefInvalidError = Error(
        "ALTINN004",
        "Data type class reference could not be found",
        "Class reference '{0}' for data type '{1}' could not be found"
    );

    internal static readonly ImmutableArray<DiagnosticDescriptor> All;

    static Diagnostics()
    {
        All = ImmutableArray.CreateRange(
            typeof(Diagnostics)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .Select(f => (DiagnosticDescriptor)f.GetValue(null))
        );
    }

    private const string DocsRoot = "https://docs.altinn.studio/app/development/analysis/";
    private const string RulesRoot = DocsRoot + "rules/";

    private static DiagnosticDescriptor Warning(string id, string title, string messageFormat) =>
        Create(id, title, messageFormat, Category.General, DiagnosticSeverity.Warning);

    private static DiagnosticDescriptor Error(string id, string title, string messageFormat) =>
        Create(id, title, messageFormat, Category.General, DiagnosticSeverity.Error);

    private static DiagnosticDescriptor Create(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity severity
    ) => new(id, title, messageFormat, category, severity, true, helpLinkUri: RulesRoot + id);

    private static class Category
    {
        public const string General = nameof(General);
    }
}

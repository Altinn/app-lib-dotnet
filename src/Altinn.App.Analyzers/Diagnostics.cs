using System.Reflection;

namespace Altinn.App.Analyzers;

internal static class Diagnostics
{
    internal static readonly DiagnosticDescriptor UnknownError = Warning(
        "ALTINN999",
        Category.General,
        "Unknown analyzer error",
        "Unknown error occurred during analysis: '{0}' {1}"
    );

    internal static readonly DiagnosticDescriptor ProjectNotFound = Warning(
        "ALTINN001",
        Category.General,
        "Altinn app project not found",
        "While starting analysis, we couldn't find the project directory - contact support"
    );

    internal static readonly DiagnosticDescriptor ApplicationMetadataFileNotFound = Warning(
        "ALTINN002",
        Category.Metadata,
        "Altinn app metadata file not found",
        "Could not find application metadata file at 'config/applicationmetadata.json'"
    );

    internal static readonly DiagnosticDescriptor FailedToParseApplicationMetadata = Warning(
        "ALTINN003",
        Category.Metadata,
        "Altinn app metadata file couldn't be parsed",
        "Could not parse application metadata file at 'config/applicationmetadata.json': '{0}' {1}"
    );

    internal static readonly DiagnosticDescriptor DataTypeClassRefInvalid = Warning(
        "ALTINN004",
        Category.Metadata,
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

    private static DiagnosticDescriptor Warning(string id, string category, string title, string messageFormat) =>
        Create(id, title, messageFormat, category, DiagnosticSeverity.Warning);

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
        public const string Metadata = nameof(Metadata);
    }
}

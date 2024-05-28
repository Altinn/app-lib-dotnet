using System.Reflection;

namespace Altinn.App.Analyzers;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor UnknownError = Warning(
        "ALTINN9999",
        Category.General,
        "Unknown analyzer error",
        "Unknown error occurred during analysis, contact support: '{0}' {1}"
    );

    public static readonly DiagnosticDescriptor ProjectNotFound = Warning(
        "ALTINN0001",
        Category.General,
        "Altinn app project not found",
        "While starting analysis, we couldn't find the project directory - contact support"
    );

    public static class ApplicationMetadata
    {
        public static readonly DiagnosticDescriptor FileNotFound = Warning(
            "ALTINN0010",
            Category.Metadata,
            "Altinn app metadata file not found",
            "Could not find application metadata file at '{0}'"
        );

        public static readonly DiagnosticDescriptor FileNotReadable = Warning(
            "ALTINN0011",
            Category.Metadata,
            "Altinn app metadata file could not be opened/read",
            "Could not open and read the application metdata file: '{0}' {1}"
        );

        public static readonly DiagnosticDescriptor ParsingFailure = Warning(
            "ALTINN0012",
            Category.Metadata,
            "Altinn app metadata file couldn't be parsed",
            "Could not parse application metadata file: '{0}' {1}"
        );

        public static readonly DiagnosticDescriptor DataTypeClassRefInvalid = Warning(
            "ALTINN0013",
            Category.Metadata,
            "Data type class reference could not be found",
            "Class reference '{0}' for data type '{1}' could not be found"
        );

        public static readonly DiagnosticDescriptor OnEntryShowRefInvalid = Warning(
            "ALTINN0014",
            Category.Metadata,
            "On entry show layout reference could not be found",
            "UI layout reference '{0}' in app metadata 'onEntry' configuration could not be resolved"
        );
    }

    public static class Layouts
    {
        public static readonly DiagnosticDescriptor FileNotFound = Warning(
            "ALTINN0040",
            Category.Metadata,
            "Altinn layout-sets file not found",
            "Could not find layout-sets file at '{0}'"
        );

        public static readonly DiagnosticDescriptor FileNotReadable = Warning(
            "ALTINN0041",
            Category.Metadata,
            "Altinn layout-sets file could not be opened/read",
            "Could not open and read the layout-sets file: '{0}' {1}"
        );

        public static readonly DiagnosticDescriptor ParsingFailure = Warning(
            "ALTINN0042",
            Category.Metadata,
            "Altinn layout-sets file couldn't be parsed",
            "Could not parse the layout-sets file: '{0}' {1}"
        );

        public static readonly DiagnosticDescriptor DataTypeRefInvalid = Warning(
            "ALTINN0043",
            Category.Metadata,
            "Data type reference in layout set could not be resolved",
            "Data type reference '{0}' configured in layout set '{1}' in the 'layout-sets.json'-file could not be resolved"
        );
    }

    public static readonly ImmutableArray<DiagnosticDescriptor> All;

    static Diagnostics()
    {
        var getDiagnostics = static (Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .Select(f => (DiagnosticDescriptor)f.GetValue(null));

        All = ImmutableArray.CreateRange(
            getDiagnostics(typeof(Diagnostics))
                .Union(getDiagnostics(typeof(ApplicationMetadata)))
                .Union(getDiagnostics(typeof(Layouts)))
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

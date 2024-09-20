using System.Reflection;

namespace Altinn.App.Internal.Analyzers;

file static class Diagnostics
{
    public static readonly ImmutableArray<DiagnosticDescriptor> All;

    static Diagnostics()
    {
        var getDiagnostics = static (Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
                .Select(f => (DiagnosticDescriptor)f.GetValue(null));

        All = ImmutableArray.CreateRange(getDiagnostics(typeof(Diagnostics)));
    }

    public static readonly DiagnosticDescriptor UnknownError = Error(
        "ALTINNINT9999",
        Category.General,
        "Unknown analyzer error",
        "Unknown error occurred during analysis: '{0}'"
    );

    public static readonly DiagnosticDescriptor DangerousConstructorInjection = Error(
        "ALTINNINT0001",
        Category.General,
        "Dangerious constructor injection",
        "Service interface '{0}' is injected into the constructor of '{1}'."
            + " This is dangerous as we couple the lifetime of '{1}' to the lifetime of the implementation of '{0}'."
            + " Use the 'AppImplementationFactory' to instantiate app implementations lazily instead."
    );

    public static readonly DiagnosticDescriptor DangerousServiceProviderServiceResolution = Error(
        "ALTINNINT0002",
        Category.General,
        "Dangerious 'IServiceProvider' service resolution",
        "App implementable service interface '{0}' is resolved through 'IServiceProvider'."
            + " App implementable interfaces are only meant to be resolved through 'AppImplementationFactory'."
    );

    private static DiagnosticDescriptor Error(string id, string category, string title, string messageFormat) =>
        Create(id, title, messageFormat, category, DiagnosticSeverity.Error);

    private static DiagnosticDescriptor Create(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity severity
    ) => new(id, title, messageFormat, category, severity, true, helpLinkUri: null);

    private static class Category
    {
        public const string General = nameof(General);
    }
}

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AppImplementationInjectionAnalyzer : DiagnosticAnalyzer
{
    private const string MarkerAttributeName = "ImplementableByAppsAttribute";
    private static readonly SymbolEqualityComparer _symbolComparer = SymbolEqualityComparer.Default;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeConstructors, SyntaxKind.ConstructorDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeDIServiceCalls, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeDIServiceCalls(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // This code checks the following forms of IServiceProvider service resolutions
        // sp.GetService<IEFormidlingMetadata>();
        // sp.GetService(typeof(IEFormidlingMetadata));
        // sp.GetRequiredService<IEFormidlingMetadata>();
        // sp.GetRequiredService(typeof(IEFormidlingMetadata));
        // ServiceProviderServiceExtensions.GetRequiredService<IEFormidlingMetadata>(_sp);
        // As we need the code to resolve through 'AppImplementationFactory' instead

        var methodSymbol =
            context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (methodSymbol is null)
            return;

        var serviceProviderType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.IServiceProvider");
        if (serviceProviderType is null)
            return;

        var arguments = invocation.ArgumentList.Arguments;
        bool isLongFormExtMethodCall = false;
        if (methodSymbol.IsExtensionMethod)
        {
            if (arguments.Count is > 0)
            {
                var firstArgType = context
                    .SemanticModel.GetTypeInfo(arguments[0].Expression, context.CancellationToken)
                    .Type;
                isLongFormExtMethodCall = _symbolComparer.Equals(firstArgType, serviceProviderType);
            }
            if (!_symbolComparer.Equals(methodSymbol.ReceiverType, serviceProviderType) && !isLongFormExtMethodCall)
                return;
        }
        else
        {
            if (!_symbolComparer.Equals(methodSymbol.ContainingType, serviceProviderType))
                return;
        }

        // check the generic form, e.g. GetService<T>()
        TypeSyntax? typeSyntax = null;
        var typeArgumentList = invocation.DescendantNodes().OfType<TypeArgumentListSyntax>().FirstOrDefault();
        if (typeArgumentList is not null)
        {
            typeSyntax = typeArgumentList.Arguments.FirstOrDefault();
        }

        // check the non-generic form, e.g. GetService(typeof(T))
        if (arguments.Count >= (isLongFormExtMethodCall ? 2 : 1))
        {
            var argument = arguments[isLongFormExtMethodCall ? 1 : 0];
            if (argument.Expression is TypeOfExpressionSyntax typeOfExpression)
                typeSyntax = typeOfExpression.Type;
        }

        if (typeSyntax is null)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken).Type;
        if (typeInfo == null)
            return;

        if (
            typeInfo.TypeKind == TypeKind.Interface
            && typeInfo.GetAttributes().Any(attr => attr.AttributeClass?.Name == MarkerAttributeName)
        )
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.DangerousServiceProviderServiceResolution,
                invocation.GetLocation(),
                typeInfo.Name
            );

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeConstructors(SyntaxNodeAnalysisContext context)
    {
        var node = (ConstructorDeclarationSyntax)context.Node;

        // This code ensures that classes dont inject app implementable interfaces eagerly

        var appImplementableTypesReferenced = new Dictionary<ITypeSymbol, CSharpSyntaxNode>(4, _symbolComparer);
        static void Process(
            SyntaxNodeAnalysisContext context,
            ExpressionSyntax syntax,
            Dictionary<ITypeSymbol, CSharpSyntaxNode> typesReferenced
        )
        {
            var type = context.SemanticModel.GetTypeInfo(syntax, context.CancellationToken).Type;
            if (type is null || type.TypeKind != TypeKind.Interface)
                return;
            if (typesReferenced.ContainsKey(type))
                return;
            if (!type.GetAttributes().Any(attr => attr.AttributeClass?.Name == MarkerAttributeName))
                return;

            typesReferenced.Add(type, syntax);
        }

        foreach (var parameter in node.ParameterList.Parameters)
        {
            var identifier = parameter.Type;
            if (identifier is null)
                continue;

            Process(context, identifier, appImplementableTypesReferenced);
        }

        if (node.Body is not null)
        {
            var identifiers = node.Body.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var identifier in identifiers)
            {
                Process(context, identifier, appImplementableTypesReferenced);
            }
        }

        foreach (var reference in appImplementableTypesReferenced)
        {
            var diagnostic = Diagnostic.Create(
                Diagnostics.DangerousConstructorInjection,
                reference.Value.GetLocation(),
                reference.Key.Name,
                node.Identifier.ValueText
            );

            context.ReportDiagnostic(diagnostic);
        }
    }
}

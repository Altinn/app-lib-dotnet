namespace Altinn.App.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HttpContextAccessorUsageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Diagnostics.CodeSmells.HttpContextAccessorUsage];

    public override void Initialize(AnalysisContext context)
    {
        var configFlags = GeneratedCodeAnalysisFlags.None;
        context.ConfigureGeneratedCodeAnalysis(configFlags);
        context.EnableConcurrentExecution();

        // Check all use of `IHttpContextAccessor`
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        if (memberAccess.Name is not IdentifierNameSyntax memberName)
            return;

        // Check if the member access is a call to IHttpContextAccessor
        if (memberName.Identifier.Text == "HttpContext")
        {
            // Check that this is the "HttpContext" property on "IHttpContextAccessor"
            var memberAccessType = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            var fullTypeName = memberAccessType?.ToString();
            if (fullTypeName == "Microsoft.AspNetCore.Http.IHttpContextAccessor.HttpContext")
            {
                // Report a diagnostic
                var diagnostic = Diagnostic.Create(
                    Diagnostics.CodeSmells.HttpContextAccessorUsage,
                    memberAccess.GetLocation()
                );
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace altinn_app_cli.v7Tov8.CodeRewriters;

/// <summary>
/// CSharpSyntaxRewriter to update the return type of methods in interface implementations
/// </summary>
/// <remarks>Does not use semantic model to ensure </remarks>
/// <param name="_interfaceName">Name of the interface</param>
/// <param name="_methodName">Name of the method to update signature for</param>
/// <param name="_prevReturnType">Previous return type (for safety check)</param>
/// <param name="_returnType"></param>
public class ReturnTypeRewriter
    (string _interfaceName, string _methodName, string _prevReturnType, string _returnType) : CSharpSyntaxRewriter
{
    /// <inheritdoc />
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Ignore any classes that don't implement `_interfaceName` (consider using semantic model to ensure correct reference)
        if (node.BaseList?.Types.Any(t => t.Type.ToString() == _interfaceName) == true)
        {
            var method = node.Members.OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.ValueText == _methodName && m.ReturnType.ToString() == _prevReturnType);
            if (method is not null)
            {
                node = node.ReplaceNode(
                    method,
                    method.WithReturnType(
                        SyntaxFactory
                            .ParseTypeName(_returnType)
                            .WithTrailingTrivia(SyntaxFactory.Space)));
            }

            
        }

        return base.VisitClassDeclaration(node);
    }
}
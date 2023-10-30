namespace altinn_app_cli.v7Tov8.CodeRewriters;

using Microsoft.CodeAnalysis;

/// <summary>
/// Interface to mark that semantic model is required in this CSharpRewriter implementation
/// </summary>
public interface ISemanticModelInjector
{
    /// <summary>
    /// Property that will be set in program before running the visitor
    /// </summary>
    public SemanticModel InjectedSemanticModel { get; set; }
}

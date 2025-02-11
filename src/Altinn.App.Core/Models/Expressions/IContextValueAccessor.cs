namespace Altinn.App.Core.Models.Expressions;

/// <summary>
/// Provide an implementation for the ["value", ?] expression
/// The expression is special, because it is only valid in contexts where there exists a value
/// * Expression validation (`["value"]` is the value to validate)
/// * Filtering (["value"] is the value that is should be compared)
/// </summary>
public interface IContextValueAccessor
{
    /// <summary>
    /// Get the value from the context
    /// </summary>
    /// <param name="function">The function that was called</param>
    /// <param name="args">List of arguments for the expression</param>
    /// <returns>The value</returns>
    object? GetValue(ExpressionFunction function, ReadOnlySpan<object?> args);
}

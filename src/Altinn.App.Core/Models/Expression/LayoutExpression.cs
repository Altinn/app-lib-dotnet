using System.Text.Json.Serialization;
using Altinn.App.Core.Expressions;

namespace Altinn.App.Core.Models.Expression;

/// <summary>
/// Model for C# representation of a Layout Expression that can be part of a layout and Evaluated with <see cref="ExpressionEvaluator" />
/// </summary>
/// <remarks>
/// All props are marked as nullable, but a valid instance has either <see cref="Function" /> and <see cref="Args" /> or <see cref="Value" />
/// </remarks>
[JsonConverter(typeof(LayoutExpressionConverter))]
public sealed class LayoutExpression
{
    /// <summary>
    /// Name of the function. Must be one those actually implemented in <see cref="ExpressionEvaluator" />
    /// </summary>
    public LayoutExpressionFunctionEnum? Function { get; set; }

    /// <summary>
    /// List of arguments to the function. These expressions will be evaluated before passed to the function.
    /// </summary>
    public List<LayoutExpression>? Args { get; set; }

    /// <summary>
    /// Some expressions are just literal values that evaluate to the same value.
    /// </summary>
    /// <remarks>
    ///  If <see cref="Value" /> isn't null, <see cref="Function" /> and <see cref="Args" /> must be
    /// </remarks>    
    public object? Value { get; set; }
}
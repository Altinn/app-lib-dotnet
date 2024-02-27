namespace Altinn.App.Core.Models.Expressions;

/// <summary>
/// Base class for holding context for running expressions
/// </summary>
public abstract class ExpressionContext
{
    /// <summary>
    /// Constructor for ExpressionContext
    /// </summary>
    protected ExpressionContext(int[]? rowIndices)
    {
        RowIndices = rowIndices;
    }

    /// <summary>
    /// The indicies for this context (in case the component is part of a repeating group)
    /// </summary>
    public int[]? RowIndices { get; }
}

/// <summary>
/// Base class for holding context for running expressions
/// </summary>
public class RowContext : ExpressionContext
{
    /// <summary>
    /// Constructor for RowContext
    /// </summary>
    public RowContext(int[]? rowIndices)
        : base(rowIndices) { }
}

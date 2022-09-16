namespace Altinn.App.Core.Implementation.Expression;

/// <summary>
/// Simple class for holding the context for <see cref="ExpressionEvaluator"/>
/// </summary>
public sealed class ComponentContext
{
    /// <summary>
    /// 
    /// </summary>
    public ComponentContext(Component component, int[]? RowIndicies)
    {
        Component = component;
        RowIndices = RowIndicies;
    }

    /// <summary>
    /// The component from <see cref="ComponentModel"/> that should be used as context
    /// </summary>
    public Component Component { get; init; }

    /// <summary>
    /// The indicies for this context (in case the component is part of a repeating group)
    /// </summary>
    public int[]? RowIndices { get; init; }
}
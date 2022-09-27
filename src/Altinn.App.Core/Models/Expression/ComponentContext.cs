namespace Altinn.App.Core.Expressions;

/// <summary>
/// Simple class for holding the context for <see cref="ExpressionEvaluator"/>
/// </summary>
public sealed class ComponentContext
{
    /// <summary>
    /// 
    /// </summary>
    public ComponentContext(BaseComponent component, int[]? RowIndicies, IEnumerable<ComponentContext> childContexts)
    {
        Component = component;
        RowIndices = RowIndicies;
        ChildContexts = childContexts;
    }

    /// <summary>
    /// The component from <see cref="ComponentModel"/> that should be used as context
    /// </summary>
    public BaseComponent Component { get; }

    /// <summary>
    /// The indicies for this context (in case the component is part of a repeating group)
    /// </summary>
    public int[]? RowIndices { get; }

    /// <summary>
    /// Contexts that logically belongs under this context (eg cell => row => group=> page)
    /// </summary>
    public IEnumerable<ComponentContext> ChildContexts { get; }
}
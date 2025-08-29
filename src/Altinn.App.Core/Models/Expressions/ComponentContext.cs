using System.Diagnostics;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components.Base;

namespace Altinn.App.Core.Models.Expressions;

/// <summary>
/// Simple class for holding the context for <see cref="ExpressionEvaluator"/>
/// </summary>
[DebuggerDisplay("{_debuggerDisplay}", Name = "{_debuggerName}")]
[DebuggerTypeProxy(typeof(DebuggerProxy))]
public sealed class ComponentContext
{
    /// <summary>
    /// Constructor for ComponentContext
    /// </summary>
    public ComponentContext(
        LayoutEvaluatorState state,
        BaseComponent? component,
        int[]? rowIndices,
        DataElementIdentifier dataElementIdentifier,
        List<ComponentContext>? childContexts = null
    )
    {
        State = state;
        DataElementIdentifier = dataElementIdentifier;
        Component = component;
        RowIndices = rowIndices;
        ChildContexts = childContexts ?? [];
        foreach (var child in ChildContexts)
        {
            child.Parent = this;
        }
    }

    /// <summary>
    /// The component from <see cref="LayoutModel"/> that should be used as context
    /// </summary>
    public BaseComponent? Component { get; }

    /// <summary>
    /// The indexes for this context (in case the component is part of a repeating group)
    /// </summary>
    public int[]? RowIndices { get; }

    private bool? _isHidden;

    /// <summary>
    /// Memoized way to check if the component is hidden
    /// </summary>
    public async Task<bool> IsHidden()
    {
        if (_isHidden.HasValue)
        {
            return _isHidden.Value;
        }
        if (Parent is not null && await Parent.IsHidden())
        {
            _isHidden = true;
            return _isHidden.Value;
        }

        // If the data has already been cleaned, rows are set to null for validation
        // but if the "hiddenRow" expression hides rows with specific values, the expression
        // might not hide the row when the data is null.
        //
        // The only reason for a row to be null is that the data has been cleaned and the row was considered hidden
        // so we just check the data and assume it was hidden.
        if (Component is RepeatingGroupRowComponent rgc && rgc.DataModelBindings.TryGetValue("group", out var binding))
        {
            var data = await State.GetModelData(binding, DataElementIdentifier, RowIndices);
            if (data is null)
            {
                _isHidden = true;
                return _isHidden.Value;
            }
        }

        var hidden = await ExpressionEvaluator.EvaluateBooleanExpression(State, this, "hidden", false);

        _isHidden = hidden;
        return _isHidden.Value;
    }

    /// <summary>
    /// Contexts that logically belongs under this context (eg cell => row => group=> page)
    /// </summary>
    public List<ComponentContext> ChildContexts { get; }

    /// <summary>
    /// Parent context or null, if this is a root context, or a context created without setting parent
    /// </summary>
    public ComponentContext? Parent { get; private set; }

    /// <summary>
    /// The LatoutEvaluatorState that this context is part of
    /// </summary>
    public LayoutEvaluatorState State { get; }

    /// <summary>
    /// The Id of the default data element in this context
    /// </summary>
    public DataElementIdentifier DataElementIdentifier { get; }

    /// <summary>
    /// Get all children and children of children of this componentContext (not including this)
    /// </summary>
    public IEnumerable<ComponentContext> Descendants
    {
        get
        {
            var stack = new Stack<ComponentContext>(ChildContexts);
            while (stack.Count != 0)
            {
                var node = stack.Pop();
                yield return node;
                foreach (var child in node.ChildContexts)
                    stack.Push(child);
            }
        }
    }

    private string _debuggerName =>
        $"{Component?.Type}" + (RowIndices is not null ? $"[{string.Join(',', RowIndices)}]" : "");
    private string _debuggerDisplay =>
        $"id:\"{Component?.Id}\"" + (ChildContexts.Count > 0 ? $" ({ChildContexts.Count} children)" : "");

    private class DebuggerProxy
    {
        private readonly ComponentContext _context;

        public DebuggerProxy(ComponentContext context)
        {
            _context = context;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public List<ComponentContext> ChildContexts => _context.ChildContexts;
        public BaseComponent? Component => _context.Component;
        public ComponentContext? Parent => _context.Parent;
        public bool? IsHidden => _context._isHidden;
        public Guid DataElementId => _context.DataElementIdentifier.Guid;
        public int[]? RowIndices => _context.RowIndices;

        public DebuggerEvaluatedExpression HiddenExpression =>
            new(_context.Component?.Hidden ?? new Expression("COMPONENT WAS NULL"), _context);

        public class DebuggerEvaluatedExpression
        {
            private readonly ComponentContext _context;
            private readonly Expression _expression;

            public DebuggerEvaluatedExpression(Expression expression, ComponentContext context)
            {
                _context = context;
                _expression = expression;
            }

            public ExpressionFunction Function => _expression.Function;
            public IEnumerable<DebuggerEvaluatedExpression>? Args =>
                _expression.Args?.Select(e => new DebuggerEvaluatedExpression(e, _context));
            public ExpressionValue EvaluationResult =>
                _expression.IsFunctionExpression
                    ? ExpressionEvaluator
                        .EvaluateExpression_internal(_context.State, _expression, _context, null)
                        .Result
                    : _expression.ValueUnion;

            public override string ToString()
            {
                return _expression.ToString();
            }
        }
    }
}

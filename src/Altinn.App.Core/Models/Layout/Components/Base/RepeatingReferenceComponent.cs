using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components.Base;

/// <summary>
/// Represents a repeating reference component in a layout.
/// This component type manages references to its child components, allowing dynamic structure in layouts.
/// </summary>
public abstract class RepeatingReferenceComponent : BaseComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatingReferenceComponent"/> class
    /// </summary>
    protected RepeatingReferenceComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId) { }

    /// <summary>
    /// Model binding for the group that defines the number of repetitions of the repeating group.
    /// </summary>
    public abstract ModelBinding GroupModelBinding { get; }

    /// <summary>
    /// The expression that determines if the row is hidden.
    /// </summary>
    public abstract Expression HiddenRow { get; }

    /// <summary>
    /// List of references to child components that are repeated for each row in the repeating group.
    /// </summary>
    public abstract IReadOnlyList<string> RepeatingChildReferences { get; }

    /// <summary>
    /// References to child components that are not repeated
    /// </summary>
    public abstract IReadOnlyList<string> NonRepeatingChildReferences { get; }

    // References to the components that are used for the child contexts of this component
    private Dictionary<string, BaseComponent>? _claimedChildrenLookup;

    // used for some tests to ensure hierarchy is correct
    internal IEnumerable<BaseComponent>? AllChildren => _claimedChildrenLookup?.Values;

    /// <inheritdoc />
    public override void ClaimChildren(
        Dictionary<string, BaseComponent> unclaimedComponents,
        Dictionary<string, string> claimedComponents
    )
    {
        if (GroupModelBinding.Field is null || RepeatingChildReferences is null || NonRepeatingChildReferences is null)
        {
            throw new UnreachableException(
                $"{GetType().Name} inherits from {nameof(RepeatingReferenceComponent)} and must initialize {nameof(GroupModelBinding)}, {nameof(RepeatingChildReferences)}, {nameof(HiddenRow)} and {nameof(NonRepeatingChildReferences)} in its constructor."
            );
        }

        var components = new Dictionary<string, BaseComponent>();
        foreach (var componentId in RepeatingChildReferences.Concat(NonRepeatingChildReferences))
        {
            if (unclaimedComponents.Remove(componentId, out var component))
            {
                claimedComponents[componentId] = Id;
            }
            else
            {
                // Invalid reference. Throw the appropriate exception.
                if (claimedComponents.TryGetValue(componentId, out var claimedComponent))
                {
                    throw new ArgumentException(
                        $"Attempted to claim child with id {componentId} to component {Id}, but it has already been claimed by {claimedComponent}."
                    );
                }
                throw new ArgumentException(
                    $"Attempted to claim child with id {componentId} to component {Id}, but the componentId does not exist"
                );
            }

            if (!components.TryAdd(component.Id, component))
            {
                throw new ArgumentException($"Component with id {component.Id} is claimed twice by {Id}.");
            }
        }

        _claimedChildrenLookup = components;
    }

    /// <inheritdoc />
    public override async Task<ComponentContext> GetContext(
        LayoutEvaluatorState state,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes,
        Dictionary<string, LayoutSetComponent> layoutsLookup
    )
    {
        if (
            _claimedChildrenLookup is null
            || RepeatingChildReferences is null
            || NonRepeatingChildReferences is null
            || GroupModelBinding.Field is null
        )
        {
            throw new InvalidOperationException(
                $"{GetType().Name} must call {nameof(ClaimChildren)} before calling {nameof(GetContext)}."
            );
        }

        var childContexts = new List<ComponentContext>();

        foreach (var componentId in NonRepeatingChildReferences)
        {
            if (_claimedChildrenLookup.TryGetValue(componentId, out var childComponent))
            {
                var childContext = await childComponent.GetContext(
                    state,
                    defaultDataElementIdentifier,
                    rowIndexes,
                    layoutsLookup
                );
                childContexts.Add(childContext);
            }
            else
            {
                throw new ArgumentException($"Child component with id {componentId} not found in claimed children.");
            }
        }

        var rowCount = await state.GetModelDataCount(GroupModelBinding, defaultDataElementIdentifier, rowIndexes) ?? 0;

        // We need to count backwards so that deleting by index works for multiple rows.
        for (int i = 0; i < rowCount; i++)
        {
            var subRowIndexes = GetSubRowIndexes(rowIndexes, i);
            var rowComponent = new RepeatingGroupRowComponent(
                $"{Id}__group_row_{i}",
                PageId,
                LayoutId,
                DataModelBindings,
                HiddenRow,
                RemoveWhenHidden
            );
            List<ComponentContext> rowChildren = [];
            foreach (var componentId in RepeatingChildReferences)
            {
                if (_claimedChildrenLookup.TryGetValue(componentId, out var childComponent))
                {
                    var childContext = await childComponent.GetContext(
                        state,
                        defaultDataElementIdentifier,
                        subRowIndexes,
                        layoutsLookup
                    );
                    rowChildren.Add(childContext);
                }
                else
                {
                    throw new ArgumentException(
                        $"Child component with id {componentId} not found in claimed children."
                    );
                }
            }

            // Insert the new row context at the beginning so that they are processed in deletion order
            childContexts.Insert(
                0,
                new ComponentContext(state, rowComponent, subRowIndexes, defaultDataElementIdentifier, rowChildren)
            );
        }

        return new ComponentContext(state, this, rowIndexes, defaultDataElementIdentifier, childContexts);
    }

    private static int[] GetSubRowIndexes(int[]? baseIndexes, int index)
    {
        if (baseIndexes is null || baseIndexes.Length == 0)
        {
            return new[] { index };
        }
        var result = new int[baseIndexes.Length + 1];
        Array.Copy(baseIndexes, result, baseIndexes.Length);
        result[^1] = index;
        return result;
    }
}

/// <summary>
/// Component for each row (not read from JSON layout, but created when generating contexts for repeating groups).
/// </summary>
public class RepeatingGroupRowComponent : BaseComponent
{
    /// <summary>
    /// Represents a dynamically created component for each row in a repeating group.
    /// These components are not directly defined in the JSON layout but are generated
    /// during runtime to manage the context of repeating group rows.
    /// </summary>
    public RepeatingGroupRowComponent(
        string id,
        string pageId,
        string layoutId,
        IReadOnlyDictionary<string, ModelBinding> dataModelBindings,
        Expression hiddenRow,
        Expression removeWhenHidden
    )
        : base(
            id,
            pageId,
            layoutId,
            "repeatingGroupRow",
            required: Expression.False,
            readOnly: Expression.False,
            hidden: hiddenRow,
            removeWhenHidden,
            dataModelBindings,
            ImmutableDictionary<string, Expression>.Empty
        ) { }

    /// <inheritdoc />
    public override Task<ComponentContext> GetContext(
        LayoutEvaluatorState state,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes,
        Dictionary<string, LayoutSetComponent> layoutsLookup
    )
    {
        // This component is not part of the layout structure, so it never creates a context.
        return Task.FromException<ComponentContext>(new NotImplementedException());
    }

    /// <inheritdoc />
    public override void ClaimChildren(
        Dictionary<string, BaseComponent> unclaimedComponents,
        Dictionary<string, string> claimedComponents
    )
    {
        // This component does not claim children from the layout.
        throw new NotImplementedException();
    }
}

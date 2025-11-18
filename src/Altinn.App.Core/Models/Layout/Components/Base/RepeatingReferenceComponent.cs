using System.Diagnostics;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components.Base;

/// <summary>
/// Represents a repeating reference component in a layout.
/// This component type manages references to its child components, allowing dynamic structure in layouts.
/// </summary>
public abstract class RepeatingReferenceComponent : BaseLayoutComponent
{
    /// <summary>
    /// Model binding for the group that defines the number of repetitions of the repeating group.
    /// </summary>
    public required ModelBinding GroupModelBinding { get; init; }

    /// <summary>
    /// The expression that determines if the row is hidden.
    /// </summary>
    public required Expression HiddenRow { get; init; }

    /// <summary>
    /// List of references to child components that are repeated for each row in the repeating group.
    /// </summary>
    public required IReadOnlyList<string> RepeatingChildReferences { get; init; }

    /// <summary>
    /// References to child components that are not repeated and comes before the repeating group
    /// </summary>
    public required IReadOnlyList<string> BeforeChildReferences { get; init; }

    /// <summary>
    /// References to child components that are not repeated and comes after the repeating group
    /// </summary>
    public required IReadOnlyList<string> AfterChildReferences { get; init; }

    /// <summary>
    /// References to the components that are used for the child contexts of this component
    /// </summary>
    protected Dictionary<string, BaseLayoutComponent>? ClaimedChildrenLookup { get; private set; }

    // used for some tests to ensure hierarchy is correct
    internal IEnumerable<BaseComponent>? AllChildren => ClaimedChildrenLookup?.Values;

    /// <inheritdoc />
    public override void ClaimChildren(
        Dictionary<string, BaseLayoutComponent> unclaimedComponents,
        Dictionary<string, string> claimedComponents
    )
    {
        if (
            GroupModelBinding.Field is null
            || RepeatingChildReferences is null
            || BeforeChildReferences is null
            || AfterChildReferences is null
        )
        {
            throw new UnreachableException(
                $"{GetType().Name} inherits from {nameof(RepeatingReferenceComponent)} and must initialize {nameof(GroupModelBinding)}, {nameof(RepeatingChildReferences)}, {nameof(HiddenRow)}, {nameof(BeforeChildReferences)} and {nameof(AfterChildReferences)} in its constructor."
            );
        }

        var components = new Dictionary<string, BaseLayoutComponent>();
        foreach (var componentId in BeforeChildReferences.Concat(RepeatingChildReferences).Concat(AfterChildReferences))
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

        ClaimedChildrenLookup = components;
    }

    internal static int[] GetSubRowIndexes(int[]? baseIndexes, int index)
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

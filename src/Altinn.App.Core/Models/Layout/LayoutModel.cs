using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Models.Layout;

/// <summary>
/// Class for handling a full layout/layoutset
/// </summary>
public class LayoutModel
{
    private readonly Dictionary<string, LayoutSetComponent> _layoutsLookup;
    private readonly LayoutSetComponent _defaultLayoutSet;

    /// <summary>
    /// Constructor for the component model that wraps multiple layouts
    /// </summary>
    /// <param name="layouts">List of layouts we need</param>
    /// <param name="defaultLayout">Optional default layout (if not just using the first)</param>
    public LayoutModel(List<LayoutSetComponent> layouts, LayoutSet? defaultLayout)
    {
        _layoutsLookup = layouts.ToDictionary(l => l.Id);
        _defaultLayoutSet = defaultLayout is not null ? _layoutsLookup[defaultLayout.Id] : layouts[0];
    }

    /// <summary>
    /// The default data type for the layout model
    /// </summary>
    public DataType DefaultDataType => _defaultLayoutSet.DefaultDataType;

    /// <summary>
    /// Get a specific component on a specific page.
    /// </summary>
    public BaseComponent GetComponent(string pageName, string componentId)
    {
        var page = _defaultLayoutSet.GetPage(pageName);

        if (!page.ComponentLookup.TryGetValue(componentId, out var component))
        {
            throw new ArgumentException($"Unknown component {componentId} on {pageName}");
        }
        return component;
    }

    /// <summary>
    /// Generate a list of <see cref="ComponentContext"/> for all components in the layout model
    /// taking repeating groups into account.
    /// </summary>
    /// <param name="state">The layoutEvaluator state to generate contexts for</param>
    public async Task<List<ComponentContext>> GenerateComponentContexts(LayoutEvaluatorState state)
    {
        var pageContexts = new List<ComponentContext>();
        foreach (var page in _defaultLayoutSet.Pages)
        {
            var defaultElementId = _defaultLayoutSet.GetDefaultDataElementId(state.Instance);
            if (defaultElementId is not null)
            {
                pageContexts.Add(await GenerateComponentContextsRecurs(page, state, defaultElementId.Value, []));
            }
        }

        return pageContexts;
    }

    private async Task<ComponentContext> GenerateComponentContextsRecurs(
        BaseComponent component,
        LayoutEvaluatorState state,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? indexes
    )
    {
        return component switch
        {
            SubFormComponent subFormComponent => await GenerateContextForSubComponent(
                state,
                subFormComponent,
                defaultDataElementIdentifier
            ),

            RepeatingGroupComponent repeatingGroupComponent => await GenerateContextForRepeatingGroup(
                state,
                repeatingGroupComponent,
                defaultDataElementIdentifier,
                indexes
            ),
            GroupComponent groupComponent => await GenerateContextForGroup(
                state,
                groupComponent,
                defaultDataElementIdentifier,
                indexes
            ),
            _ => new ComponentContext(
                state,
                component,
                indexes?.Length > 0 ? indexes : null,
                defaultDataElementIdentifier,
                []
            ),
        };
    }

    private async Task<ComponentContext> GenerateContextForGroup(
        LayoutEvaluatorState state,
        GroupComponent groupComponent,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? indexes
    )
    {
        List<ComponentContext> children = [];
        foreach (var child in groupComponent.Children)
        {
            children.Add(await GenerateComponentContextsRecurs(child, state, defaultDataElementIdentifier, indexes));
        }

        return new ComponentContext(
            state,
            groupComponent,
            indexes?.Length > 0 ? indexes : null,
            defaultDataElementIdentifier,
            children
        );
    }

    private async Task<ComponentContext> GenerateContextForRepeatingGroup(
        LayoutEvaluatorState state,
        RepeatingGroupComponent repeatingGroupComponent,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? indexes
    )
    {
        var children = new List<ComponentContext>();
        if (repeatingGroupComponent.DataModelBindings.TryGetValue("group", out var groupBinding))
        {
            var repeatingGroupRowComponent = new RepeatingGroupRowComponent(
                $"{repeatingGroupComponent.Id}_{Guid.NewGuid()}", // Ensure globally unique id (consider using altinnRowId)
                repeatingGroupComponent.DataModelBindings,
                repeatingGroupComponent.HiddenRow,
                repeatingGroupComponent
            );
            var rowLength = await state.GetModelDataCount(groupBinding, defaultDataElementIdentifier, indexes) ?? 0;
            // We add rows in reverse order, so that we can remove them without affecting the indexes of the remaining rows
            foreach (var index in Enumerable.Range(0, rowLength).Reverse())
            {
                // concatenate [...indexes, index]
                var subIndexes = new int[(indexes?.Length ?? 0) + 1];
                indexes?.CopyTo(subIndexes.AsSpan());
                subIndexes[^1] = index;

                var rowChildren = new List<ComponentContext>();
                foreach (var child in repeatingGroupComponent.Children)
                {
                    rowChildren.Add(
                        await GenerateComponentContextsRecurs(child, state, defaultDataElementIdentifier, subIndexes)
                    );
                }

                children.Add(
                    new ComponentContext(
                        state,
                        repeatingGroupRowComponent,
                        subIndexes,
                        defaultDataElementIdentifier,
                        rowChildren
                    )
                );
            }
        }

        return new ComponentContext(
            state,
            repeatingGroupComponent,
            indexes?.Length > 0 ? indexes : null,
            defaultDataElementIdentifier,
            children
        );
    }

    private async Task<ComponentContext> GenerateContextForSubComponent(
        LayoutEvaluatorState state,
        SubFormComponent subFormComponent,
        DataElementIdentifier defaultDataElementIdentifier
    )
    {
        List<ComponentContext> children = [];
        var layoutSetId = subFormComponent.LayoutSetId;
        if (!_layoutsLookup.TryGetValue(layoutSetId, out var layout))
        {
            throw new InvalidOperationException(
                $"Layout set {layoutSetId} not found. Valid layout sets are: {string.Join(", ", _layoutsLookup.Keys)}"
            );
        }
        var dataElementsForSubForm = state.Instance.Data.Where(d => d.DataType == layout.DefaultDataType.Id);
        foreach (var dataElement in dataElementsForSubForm)
        {
            foreach (var page in layout.Pages)
            {
                // "Subform" does not support "hiddenRow", so we don't need to create a context for each data element/row
                children.Add(await GenerateComponentContextsRecurs(page, state, dataElement, indexes: null));
            }
        }

        return new ComponentContext(state, subFormComponent, null, defaultDataElementIdentifier, children);
    }

    internal DataElementIdentifier? GetDefaultDataElementId(Instance instance)
    {
        return _defaultLayoutSet.GetDefaultDataElementId(instance);
    }
}

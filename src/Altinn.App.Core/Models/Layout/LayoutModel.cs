using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.Core.Models.Expressions;
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
    /// Generate a list of <see cref="ComponentContext"/> for all components in the layout model
    /// taking repeating groups into account.
    /// </summary>
    /// <param name="instance">The instance with data element information</param>
    /// <param name="dataModel">The data model to use for repeating groups</param>
    [Obsolete(
        "Use GenerateComponentContexts(IInstanceDataAccessor) instead. This method will be removed in a future version."
    )]
    public async Task<List<ComponentContext>> GenerateComponentContexts(Instance instance, DataModel dataModel) =>
        await GenerateComponentContexts(dataModel.InstanceDataAccessor);

    /// <summary>
    /// Generate a list of <see cref="ComponentContext"/> for all components in the layout model
    /// taking repeating groups into account.
    /// </summary>
    public async Task<List<ComponentContext>> GenerateComponentContexts(IInstanceDataAccessor dataAccessor)
    {
        var pageContexts = new List<ComponentContext>();
        foreach (var page in _defaultLayoutSet.Pages)
        {
            var defaultElementId = _defaultLayoutSet.GetDefaultDataElementId(dataAccessor.Instance);
            if (defaultElementId is not null)
            {
                pageContexts.Add(await page.GetContext(dataAccessor, defaultElementId.Value, null, _layoutsLookup));
            }
        }

        return pageContexts;
    }

    internal DataElementIdentifier? GetDefaultDataElementId(Instance instance)
    {
        return _defaultLayoutSet.GetDefaultDataElementId(instance);
    }
}

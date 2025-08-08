using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components.Base;

/// <summary>
/// Simple base component that does not have any references to other components.
/// </summary>
public abstract class NoReferenceComponent : BaseComponent
{
    /// <inheritdoc />
    public NoReferenceComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId) { }

    /// <summary>
    /// No children to claim for NoReferenceComponent
    /// </summary>
    public override void ClaimChildren(
        Dictionary<string, BaseComponent> unclaimedComponents,
        Dictionary<string, string> claimedComponents
    ) { }

    /// <summary>
    /// No child contexts to return for NoReferenceComponent
    /// </summary>
    public override Task<ComponentContext> GetContext(
        IInstanceDataAccessor dataAccessor,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes,
        Dictionary<string, LayoutSetComponent> layoutsLookup
    ) => Task.FromResult(new ComponentContext(this, rowIndexes, defaultDataElementIdentifier));
}

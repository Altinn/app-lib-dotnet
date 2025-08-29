using System.Text.Json;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Represents an unknown or unrecognized component in a layout. UnknownComponent serves as a placeholder
/// for components that do not match any predefined or supported type.
/// </summary>
public sealed class UnknownComponent : Base.NoReferenceComponent
{
    /// <summary>
    /// Constructor for UnknownComponent
    /// </summary>
    public UnknownComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId) { }
}

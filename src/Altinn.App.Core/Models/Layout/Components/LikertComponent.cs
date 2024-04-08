using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Special component for handling likert scale components
/// </summary>
public class LikertComponent : BaseComponent
{
    /// <summary>
    /// The id of the option list used as headers for this likert component
    /// </summary>
    public string OptionId { get; set; }

    /// <summary>
    /// Constructor that initializes all properties of the component
    /// </summary>
    public LikertComponent(string id, string type, string optionId, IReadOnlyDictionary<string, string>? dataModelBindings, Expression? hidden, Expression? required, Expression? readOnly, IReadOnlyDictionary<string, string>? additionalProperties)
        : base(id, type, dataModelBindings, hidden, required, readOnly, additionalProperties)
    {
        OptionId = optionId;
    }
}
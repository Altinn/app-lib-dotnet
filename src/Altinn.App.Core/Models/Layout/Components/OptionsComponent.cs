using System.Text.Json;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Custom component for handeling the special fields that represents an option.
/// </summary>
public sealed class OptionsComponent : Base.NoReferenceComponent
{
    /// <summary>
    /// The ID that references <see cref="Altinn.App.Core.Features.IAppOptionsProvider.Id" /> and <see cref="Altinn.App.Core.Features.IInstanceAppOptionsProvider.Id" />
    /// </summary>
    public string? OptionsId { get; }

    /// <summary>
    /// Alternaltive to <see cref="OptionsId" /> where the options are listed inline instead of referencing an external generator
    /// </summary>
    public List<AppOption>? Options { get; }

    /// <summary>
    /// Alternaltive to <see cref="OptionsId" /> where the options are sourced from a repeating group in the datamodel
    /// </summary>
    public OptionsSource? OptionsSource { get; }

    /// <summary>
    /// Is the component referencing a secure code list (uses security context of the instance)
    /// </summary>
    public bool Secure { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    public OptionsComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId)
    {
        if (componentElement.TryGetProperty("optionsId", out JsonElement optionsIdElement))
        {
            OptionsId = optionsIdElement.GetString();
        }

        if (componentElement.TryGetProperty("options", out JsonElement optionsElement))
        {
            Options =
                optionsElement.Deserialize<List<AppOption>>()
                ?? throw new JsonException("Failed to deserialize options in OptionsComponent.");
        }

        if (componentElement.TryGetProperty("source", out JsonElement optionsSourceElement))
        {
            OptionsSource =
                optionsSourceElement.Deserialize<OptionsSource>()
                ?? throw new JsonException("Failed to deserialize optionsSource in OptionsComponent.");
        }

        Secure =
            componentElement.TryGetProperty("secure", out JsonElement secureElement)
            && secureElement.ValueKind == JsonValueKind.True;

        if (OptionsId is null && Options is null && OptionsSource is null)
        {
            throw new JsonException(
                $"\"optionsId\" or \"options\" or \"source\" is required on checkboxes, radiobuttons and dropdowns in component {pageId}.{Id}"
            );
        }
        if (OptionsId is not null && Options is not null)
        {
            throw new JsonException("\"optionsId\" and \"options\" can't both be specified");
        }
        if (OptionsId is not null && OptionsSource is not null)
        {
            throw new JsonException("\"optionsId\" and \"source\" can't both be specified");
        }
        if (OptionsSource is not null && Options is not null)
        {
            throw new JsonException("\"source\" and \"options\" can't both be specified");
        }
        if (Options is not null && Secure)
        {
            throw new JsonException("\"secure\": true is invalid for components with literal \"options\"");
        }
        if (OptionsSource is not null && Secure)
        {
            throw new JsonException(
                "\"secure\": true is invalid for components that reference a repeating group \"source\""
            );
        }
    }
}

/// <summary>
/// This is an optional child element of <see cref="OptionsComponent" /> that specifies that
/// </summary>
public record OptionsSource
{
    /// <summary>
    /// Constructor for <see cref="OptionsSource" />
    /// </summary>
    public OptionsSource(string group, string value)
    {
        Group = group;
        Value = value;
    }

    /// <summary>
    /// the group field in the data model to base the options on
    /// </summary>
    public string Group { get; }

    /// <summary>
    /// a reference to a field in the group that should be used as the option value. Notice that we set up this [{0}] syntax. Here the {0} will be replaced by each index of the group.
    /// </summary>
    /// <remarks>
    /// Notice that the value field must be unique for each element. If the repeating group does not contain a field which is unique for each item
    /// it is recommended to add a field to the data model that can be used as identificator, for instance a GUID.
    /// </remarks>
    public string Value { get; }
}

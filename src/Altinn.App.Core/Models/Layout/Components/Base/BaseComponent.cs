using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components.Base;

/// <summary>
/// Abstract base class for all components in a layout.
/// This class provides common properties and methods that all components should implement.
/// </summary>
public abstract class BaseComponent
{
    /// <summary>
    /// Constructor that initializes from a JsonElement
    /// </summary>
    protected BaseComponent(JsonElement componentElement, string pageId, string layoutId)
    {
        if (
            !componentElement.TryGetProperty("id", out JsonElement idElement)
            || idElement.ValueKind != JsonValueKind.String
        )
        {
            throw new JsonException($"Component must have a string 'id' property. Was {idElement.ValueKind}");
        }

        Id = idElement.GetString() ?? throw new UnreachableException();

        if (
            !componentElement.TryGetProperty("type", out JsonElement typeElement)
            || typeElement.ValueKind != JsonValueKind.String
        )
        {
            throw new JsonException($"Component must have a string 'type' property. Was {typeElement.ValueKind}");
        }

        Type = typeElement.GetString() ?? throw new UnreachableException();

        DataModelBindings = DeserializeModelBindings(componentElement);

        TextResourceBindings = DeserializeTextResourceBindings(componentElement);

        Hidden = ParseExpression(componentElement, "hidden");
        Required = ParseExpression(componentElement, "required");
        ReadOnly = ParseExpression(componentElement, "readOnly");
        RemoveWhenHidden = ParseExpression(componentElement, "removeWhenHidden");

        PageId = pageId;
        LayoutId = layoutId;
    }

    /// <summary>
    /// Explicit constructor for BaseComponent, used by derived classes that do not need to parse from JsonElement.
    /// </summary>
    protected BaseComponent(
        string id,
        string pageId,
        string layoutId,
        string type,
        Expression required,
        Expression readOnly,
        Expression hidden,
        Expression removeWhenHidden,
        IReadOnlyDictionary<string, ModelBinding> dataModelBindings,
        IReadOnlyDictionary<string, Expression> textResourceBindings
    )
    {
        Id = id;
        PageId = pageId;
        LayoutId = layoutId;
        Type = type;
        DataModelBindings = dataModelBindings;
        TextResourceBindings = textResourceBindings;
        Required = required;
        ReadOnly = readOnly;
        Hidden = hidden;
        RemoveWhenHidden = removeWhenHidden;
    }

    /// <summary>
    /// ID of the component (or pageName for pages)
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The name of the page for the component
    /// </summary>
    public string PageId { get; }

    /// <summary>
    /// Name of the layout that this component is part of
    /// </summary>
    public string LayoutId { get; }

    /// <summary>
    /// Component type as written in the json file
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Layout Expression that can be evaluated to see if the component should be hidden
    /// </summary>
    public Expression Hidden { get; }

    /// <summary>
    /// Signal whether the data referenced by this component should be removed at the end of the task.
    /// </summary>
    public Expression RemoveWhenHidden { get; }

    /// <summary>
    /// Layout Expression that can be evaluated to see if the component should be required
    /// </summary>
    public Expression Required { get; }

    /// <summary>
    /// Layout Expression that can be evaluated to see if the component should be readOnly
    /// </summary>
    public Expression ReadOnly { get; }

    /// <summary>
    /// Data model bindings for the component or group
    /// </summary>
    public IReadOnlyDictionary<string, ModelBinding> DataModelBindings { get; }

    /// <summary>
    /// The text resource bindings for the component.
    /// </summary>
    public IReadOnlyDictionary<string, Expression> TextResourceBindings { get; }

    /// <summary>
    /// Creates a context for the component based on the provided parameters.
    /// </summary>
    /// <returns>A <see cref="ComponentContext"/> instance representing the current context of the component.</returns>
    public abstract Task<ComponentContext> GetContext(
        LayoutEvaluatorState state,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes,
        Dictionary<string, LayoutSetComponent> layoutsLookup
    );

    /// <summary>
    /// Claims child components based on the provided references and updates the lookup dictionaries.
    /// </summary>
    /// <param name="unclaimedComponents">
    /// A dictionary of unclaimed components, where keys are component IDs and values are the corresponding component instances.
    /// </param>
    /// <param name="claimedComponents">
    /// A dictionary to track claimed components, where the keys are component IDs and values are the IDs of the components that claimed them.
    /// </param>
    public abstract void ClaimChildren(
        Dictionary<string, BaseComponent> unclaimedComponents,
        Dictionary<string, string> claimedComponents
    );

    /// <summary>
    /// Helper method to parse an expression from a JSON element.
    /// </summary>
    protected static Expression ParseExpression(JsonElement componentElement, string property)
    {
        if (componentElement.TryGetProperty(property, out var expressionElement))
        {
            return ExpressionConverter.ReadStatic(expressionElement);
        }

        return new Expression(ExpressionValue.Undefined);
    }

    private static IReadOnlyDictionary<string, ModelBinding> DeserializeModelBindings(JsonElement element)
    {
        if (
            !element.TryGetProperty("dataModelBindings", out JsonElement dataModelBindingsElement)
            || dataModelBindingsElement.ValueKind == JsonValueKind.Null
        )
        {
            // If the property is not present or is null, return an empty dictionary
            return ImmutableDictionary<string, ModelBinding>.Empty;
        }

        var modelBindings = new Dictionary<string, ModelBinding>();
        if (dataModelBindingsElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                $"Unexpected JSON token type '{dataModelBindingsElement.ValueKind}' for \"dataModelBindings\", expected '{nameof(JsonValueKind.Object)}'"
            );
        }

        foreach (var property in dataModelBindingsElement.EnumerateObject())
        {
            modelBindings[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => new ModelBinding
                {
                    Field = property.Value.GetString() ?? throw new UnreachableException(),
                },
                JsonValueKind.Object => property.Value.Deserialize<ModelBinding>(),
                _ => throw new JsonException("dataModelBindings must be a string or an object"),
            };
        }

        return modelBindings;
    }

    private static Dictionary<string, Expression> DeserializeTextResourceBindings(JsonElement element)
    {
        if (
            !element.TryGetProperty("textResourceBindings", out JsonElement textResourceBindingsElement)
            || textResourceBindingsElement.ValueKind == JsonValueKind.Null
        )
        {
            return [];
        }
        if (textResourceBindingsElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                $"Unexpected JSON token type '{textResourceBindingsElement.ValueKind}' for \"textResourceBindings\", expected '{nameof(JsonValueKind.Object)}'"
            );
        }

        return textResourceBindingsElement.Deserialize<Dictionary<string, Expression>>() ?? [];
    }

    /// <summary>
    /// Extracts the child component IDs from a JSON element, stripping any multipage group index.
    /// </summary>
    protected List<string> GetChildrenWithoutMultipageGroupIndex(JsonElement component, string property)
    {
        if (!component.TryGetProperty(property, out JsonElement children) || children.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Component of {Type} must have a \"{property}\" property of type array.");
        }

        return children.EnumerateArray().Select(StripPageIndexForGroupChild).ToList();
    }

    private static string StripPageIndexForGroupChild(JsonElement child)
    {
        // Group children on multipage groups have the format "pageNumber:childId" (eg "1:child1")
        // These numbers can just be ignored for backend processing, so we strip them out.
        if (child.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Each child in the \"children\" array must be a string.");
        }
        // ! we just checked the value kind, so we can safely use GetString()
        var childId = child.GetString()!;
        var index = childId.IndexOf(':');
        if (index != -1 && childId[..index].All(c => c is >= '0' and <= '9'))
        {
            // Strip the index if everything before the colon is a number
            return childId[(index + 1)..];
        }

        return childId;
    }
}

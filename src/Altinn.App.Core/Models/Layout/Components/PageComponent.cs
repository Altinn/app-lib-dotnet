using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Component like object to add Page as a group like object
/// </summary>
public sealed class PageComponent : Base.BaseComponent
{
    /// <summary>
    /// Constructor for PageComponent
    /// </summary>
    public PageComponent(JsonElement outerElement, string pageId, string layoutId)
        : base(
            pageId,
            pageId,
            layoutId,
            "page",
            required: Expression.False,
            readOnly: Expression.False,
            hidden: ParseHiddenExpressionAndValidateJsonElement(outerElement, out JsonElement dataElement),
            removeWhenHidden: Expression.Null,
            dataModelBindings: ImmutableDictionary<string, ModelBinding>.Empty,
            textResourceBindings: ImmutableDictionary<string, Expression>.Empty
        )
    {
        if (
            !dataElement.TryGetProperty("layout", out JsonElement componentsElement)
            || componentsElement.ValueKind != JsonValueKind.Array
        )
        {
            throw new JsonException("PageComponent must have a \"layout\" property of type array.");
        }

        List<Base.BaseComponent> componentList = [];

        foreach (var componentElement in componentsElement.EnumerateArray())
        {
            if (componentElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Each component in the \"layout\" array must be an object.");
            }

            if (
                !componentElement.TryGetProperty("type", out JsonElement typeElement)
                || typeElement.ValueKind != JsonValueKind.String
            )
            {
                throw new JsonException(
                    "Each component in the \"layout\" must have a \"type\" property of type string."
                );
            }

            var type = typeElement.GetString() ?? throw new UnreachableException();

            var maxCount =
                componentElement.TryGetProperty("maxCount", out JsonElement maxCountElement)
                && maxCountElement.ValueKind == JsonValueKind.Number
                    ? maxCountElement.GetInt32()
                    : 1;
            // ensure maxCount is positive
            if (maxCount < 0)
            {
                throw new JsonException(
                    $"Component {layoutId}.{pageId}.{Id} has invalid maxCount={maxCount}, must be positive."
                );
            }

            Base.BaseComponent component = type.ToLowerInvariant() switch
            {
                "group" when maxCount == 1 => new NonRepeatingGroupComponent(componentElement, pageId, layoutId),
                "group" => new RepeatingGroupComponent(componentElement, pageId, layoutId, maxCount),
                "repeatinggroup" => new RepeatingGroupComponent(componentElement, pageId, layoutId, maxCount),
                "accordion" => new NonRepeatingGroupComponent(componentElement, pageId, layoutId),
                "grid" => new GridComponent(componentElement, pageId, layoutId),
                "subform" => new SubFormComponent(componentElement, pageId, layoutId),
                "tabs" => new TabsComponent(componentElement, pageId, layoutId),
                "cards" => new CardsComponent(componentElement, pageId, layoutId),
                "checkboxes" => new OptionsComponent(componentElement, pageId, layoutId),
                "radiobuttons" => new OptionsComponent(componentElement, pageId, layoutId),
                "dropdown" => new OptionsComponent(componentElement, pageId, layoutId),
                "multipleselect" => new OptionsComponent(componentElement, pageId, layoutId),
                _ => new UnknownComponent(componentElement, pageId, layoutId),
            };

            componentList.Add(component);
        }

        var pageComponentLookup = new Dictionary<string, Base.BaseComponent>(StringComparer.Ordinal);
        foreach (var c in componentList)
        {
            if (!pageComponentLookup.TryAdd(c.Id, c))
            {
                throw new JsonException($"Duplicate component id '{c.Id}' on page '{pageId}' in layout '{layoutId}'.");
            }
        }

        Dictionary<string, string> claimedComponentIds = []; // Keep track of claimed components

        // Let all components on the page claim their children
        foreach (var component in componentList)
        {
            component.ClaimChildren(pageComponentLookup, claimedComponentIds);
        }

        // Preserve order but remove components that have been claimed
        Components = componentList.Where(c => !claimedComponentIds.ContainsKey(c.Id)).ToList();
    }

    // Silly way to run code before the base constructor is called.
    private static Expression ParseHiddenExpressionAndValidateJsonElement(
        JsonElement outerElement,
        out JsonElement dataElement
    )
    {
        if (outerElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Layout file must be an object.");
        }

        if (!outerElement.TryGetProperty("data", out dataElement) || dataElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Layout file must have a \"data\" property of type object.");
        }

        return ParseExpression(dataElement, "hidden");
    }

    /// <summary>
    /// List of the components that are part of this page.
    /// </summary>
    public IReadOnlyList<Base.BaseComponent> Components { get; private init; }

    /// <inheritdoc />
    public override async Task<ComponentContext> GetContext(
        LayoutEvaluatorState state,
        DataElementIdentifier defaultDataElementIdentifier,
        int[]? rowIndexes,
        Dictionary<string, LayoutSetComponent> layoutsLookup
    )
    {
        List<ComponentContext> childContexts = [];
        foreach (var component in Components)
        {
            childContexts.Add(
                await component.GetContext(state, defaultDataElementIdentifier, rowIndexes, layoutsLookup)
            );
        }

        return new ComponentContext(state, this, rowIndexes, defaultDataElementIdentifier, childContexts);
    }

    /// <summary>
    /// For PageComponent, you need to call RunClaimChidren to claim children for all components on the page.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public override void ClaimChildren(
        Dictionary<string, Base.BaseComponent> unclaimedComponents,
        Dictionary<string, string> claimedComponents
    )
    {
        throw new NotImplementedException();
    }
}

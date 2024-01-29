using System.Text.Json;
using System.Text.Json.Nodes;

namespace altinn_app_cli.fev3tov4.LayoutRewriter;

/// <summary>
/// Cleans up properties that are no longer allowed
/// </summary>
class PropertyCleanupMutator : ILayoutMutator
{
    public override IMutationResult Mutate(
        JsonObject component,
        Dictionary<string, JsonObject> componentLookup
    )
    {
        if (
            !component.TryGetPropertyValue("type", out var typeNode)
            || typeNode is not JsonValue typeValue
            || typeValue.GetValueKind() != JsonValueKind.String
            || typeValue.GetValue<string>() is var type && type == null
        )
        {
            return new ErrorResult() { Message = "Unable to parse component type" };
        }

        var formComponentTypes = new List<string>() {"AddressComponent", "CheckBoxes", "Custom", "Datepicker", "Dropdown", "FileUpload", "FileUploadWithTag", "Grid", "Input", "Likert", "List", "Map", "MultipleSelect", "RadioButtons", "TextArea"};

        bool changed = false;

        if (component.ContainsKey("componentType"))
        {
            component.Remove("componentType");
            changed = true;
        }

        if (component.ContainsKey("textResourceId"))
        {
            component.Remove("textResourceId");
            changed = true;
        }

        if (component.ContainsKey("customType"))
        {
            component.Remove("customType");
            changed = true;
        }

        if (component.ContainsKey("description"))
        {
            component.Remove("description");
            changed = true;
        }

        if (type == "Summary" && component.ContainsKey("pageRef"))
        {
            component.Remove("pageRef");
            changed = true;
        }

        if (!formComponentTypes.Contains(type) && component.ContainsKey("dataModelBindings"))
        {
            component.Remove("dataModelBindings");
            changed = true;
        }

        if (changed)
        {
            return new ReplaceResult() { Component = component };
        }
        return new SkipResult();
    }
}

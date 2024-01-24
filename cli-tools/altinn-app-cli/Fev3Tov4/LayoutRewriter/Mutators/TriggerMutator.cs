using System.Text.Json;
using System.Text.Json.Nodes;

namespace altinn_app_cli.fev3tov4.LayoutRewriter;

/// <summary>
/// Upgrades trigger property
/// Should be run after group component mutations
/// </summary>
class TriggerMutator : ILayoutMutator
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

        // TODO: Do we need to add standard validations to all components? Like options based components?
        var formComponentTypes = new List<string>() {"AddressComponent", "CheckBoxes", "Custom", "Datepicker", "Dropdown", "FileUpload", "FileUploadWithTag", "Grid", "Input", "Likert", "List", "Map", "MultipleSelect", "RadioButtons", "TextArea"};

        if (formComponentTypes.Contains(type))
        {
            if (component.TryGetPropertyValue("triggers", out var triggersNode))
            {
                component.Remove("triggers");

                if (triggersNode is JsonArray triggersArray && triggersArray.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validation"))
                {
                    component.Add("showValidations", JsonNode.Parse(@"[""AllExceptRequired""]"));
                    return new ReplaceResult() { Component = component };
                }
            }
            component.Add("showValidations", JsonNode.Parse(@"[""Schema"", ""Component""]"));
            return new ReplaceResult() { Component = component };
        }

        if (type == "RepeatingGroup") 
        {
            if (component.TryGetPropertyValue("triggers", out var triggersNode))
            {
                component.Remove("triggers");

                if (triggersNode is JsonArray triggersArray && 
                    (
                        triggersArray.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validation")
                        || triggersArray.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validateRow") 
                    )
                )
                {
                    component.Add("validateOnSaveRow", JsonNode.Parse(@"[""All""]"));
                    return new ReplaceResult() { Component = component };
                }
            }
        }

        if (type == "NavigationButtons") 
        {
            if (component.TryGetPropertyValue("triggers", out var triggersNode))
            {
                component.Remove("triggers");

                if (triggersNode is JsonArray triggersArray1 && triggersArray1.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validatePage")) {
                    component.Add("validateOnNext", JsonNode.Parse(@"{""page"": ""current"", ""show"": [""All""]}"));
                    return new ReplaceResult() { Component = component };
                }

                if (triggersNode is JsonArray triggersArray2 && triggersArray2.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validateAllPages")) {
                    component.Add("validateOnNext", JsonNode.Parse(@"{""page"": ""all"", ""show"": [""All""]}"));
                    return new ReplaceResult() { Component = component };
                }

                if (triggersNode is JsonArray triggersArray3 && triggersArray3.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validateCurrentAndPreviousPages")) {
                    component.Add("validateOnNext", JsonNode.Parse(@"{""page"": ""currentAndPrevious"", ""show"": [""All""]}"));
                    return new ReplaceResult() { Component = component };
                }
            }
        }

        if (type == "NavigationBar") 
        {
            if (component.TryGetPropertyValue("triggers", out var triggersNode))
            {
                component.Remove("triggers");

                if (triggersNode is JsonArray triggersArray1 && triggersArray1.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validatePage")) {
                    component.Add("validateOnForward", JsonNode.Parse(@"{""page"": ""current"", ""show"": [""All""]}"));
                    return new ReplaceResult() { Component = component };
                }

                if (triggersNode is JsonArray triggersArray2 && triggersArray2.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validateAllPages")) {
                    component.Add("validateOnForward", JsonNode.Parse(@"{""page"": ""all"", ""show"": [""All""]}"));
                    return new ReplaceResult() { Component = component };
                }

                if (triggersNode is JsonArray triggersArray3 && triggersArray3.ToList().Exists(x => x is JsonValue v && v.GetValueKind() == JsonValueKind.String && v.GetValue<string>() == "validateCurrentAndPreviousPages")) {
                    component.Add("validateOnForward", JsonNode.Parse(@"{""page"": ""currentAndPrevious"", ""show"": [""All""]}"));
                    return new ReplaceResult() { Component = component };
                }
            }
        }

        return new SkipResult();
    }
}

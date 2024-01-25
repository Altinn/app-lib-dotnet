using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace altinn_app_cli.fev3tov4.LayoutRewriter;

/// <summary>
/// Reads all layout files and applies a set of mutators to them before writing them back
/// This class requires that the app has already been converted to using layout sets
/// </summary>
class LayoutMutator
{
    private readonly IList<string> warnings = new List<string>();
    private Dictionary<string, JsonObject> layoutCollection = new Dictionary<string, JsonObject>();
    private readonly string uiFolder;

    public LayoutMutator(string uiFolder)
    {
        this.uiFolder = uiFolder;
    }

    public IList<string> GetWarnings()
    {
        return warnings;
    }

    public void ReadAllLayoutFiles()
    {
        var layoutSets = Directory.GetDirectories(uiFolder);
        foreach (var layoutSet in layoutSets)
        {
            var layoutFiles = Directory.GetFiles(Path.Join(layoutSet, "layouts"), "*.json");
            foreach (var layoutFile in layoutFiles)
            {
                var layoutText = File.ReadAllText(layoutFile);
                var layoutJson = JsonNode.Parse(layoutText);

                if (layoutJson is not JsonObject layoutJsonObject)
                {
                    warnings.Add($"Unable to parse {layoutFile} as a json object, skipping");
                    continue;
                }

                layoutCollection.Add(layoutFile, layoutJsonObject);
            }
        }
    }

    public void Mutate(ILayoutMutator mutator)
    {
        foreach ((var filePath, var layoutJson) in layoutCollection)
        {
            var compactFilePath = string.Join(Path.DirectorySeparatorChar, filePath.Split(Path.DirectorySeparatorChar)[^3..]);
            var components = new List<JsonObject>();
            var componentLookup = new Dictionary<string, JsonObject>();

            layoutJson.TryGetPropertyValue("data", out var dataNode);
            if (dataNode is not JsonObject dataObject)
            {
                warnings.Add($"Unable to parse data node in {compactFilePath}, expected an object");
                continue;
            }

            dataObject.TryGetPropertyValue("layout", out var layoutNode);
            if (layoutNode is not JsonArray layoutArray)
            {
                warnings.Add($"Unable to parse layout node in {compactFilePath}, expected an array");
                continue;
            }

            foreach (var componentNode in layoutArray)
            {
                if (componentNode is not JsonObject componentObject)
                {
                    warnings.Add($"Unable to parse component {componentNode?.ToJsonString()} in {compactFilePath}, expected an object");
                    continue;
                }

                var componentId = componentObject.TryGetPropertyValue("id", out var idNode);
                if (
                    idNode is not JsonValue idValue
                    || idValue.GetValueKind() != JsonValueKind.String
                )
                {
                    warnings.Add($"Unable to parse id {idNode?.ToJsonString()} in {compactFilePath}, expected a string");
                    continue;
                }

                var id = idValue.GetValue<string>();

                if (componentLookup.ContainsKey(id)) {
                    warnings.Add($"Duplicate id {id} in {compactFilePath}, skipping upgrade of component");
                    continue;
                }

                components.Add(componentObject);
                componentLookup.Add(id, componentObject);
            }

            foreach (var component in components)
            {
                var result = mutator.Mutate(component.DeepClone().AsObject(), componentLookup);
                if (result is SkipResult)
                {
                    continue;
                }

                if (result is ErrorResult errorResult)
                {
                    warnings.Add($"Updating component {component["id"]} in {compactFilePath} failed with the message: {errorResult.Message}");
                    continue;
                }

                if (result is DeleteResult deleteResult)
                {
                    if (deleteResult.Warnings.Count > 0)
                    {
                        warnings.Add($"Updating component {component["id"]} in {compactFilePath} resulted in the following warnings: {string.Join(", ", deleteResult.Warnings)}");
                    }
                    layoutArray.Remove(component);
                    continue;
                }

                if (result is ReplaceResult replaceResult)
                {
                    if (replaceResult.Warnings.Count > 0)
                    {
                        warnings.Add($"Updating component {component["id"]} in {compactFilePath} resulted in the following warnings: {string.Join(", ", replaceResult.Warnings)}");
                    }
                    var index = layoutArray.IndexOf(component);
                    layoutArray.RemoveAt(index);
                    layoutArray.Insert(index, replaceResult.Component);
                    continue;
                }
            }
        }
    }

    public async Task WriteAllLayoutFiles()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        await Task.WhenAll(
            layoutCollection.Select(async layoutTuple =>
            {
                layoutTuple.Deconstruct(out var filePath, out var layoutJson);

                var layoutText = layoutJson.ToJsonString(options);
                await File.WriteAllTextAsync(filePath, layoutText);
            })
        );
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Implementation.Expression;

[JsonConverter(typeof(ComponentModelConverter))]
public class ComponentModel
{
    public Dictionary<string, ComponentPage> Pages { get; init; } = new Dictionary<string, ComponentPage>();

    public Component GetComponent(string pageName, string componentId)
    {
        if (!Pages.TryGetValue(pageName, out var page))
        {
            throw new Exception($"Unknown page name {pageName}");
        }

        if (!page.ComponentDictionary.TryGetValue(componentId, out var component))
        {
            throw new Exception($"Unknown component {componentId} on {pageName}");
        }
        return component;
    }

    public object? GetComponentData(string componentId, ComponentContext context, IDataModelAccessor dataModel)
    {
        if (context.Component.Group is not null)
        {
            throw new NotImplementedException("Component lookup for groups not implemented");
        }

        var component = GetComponent(context.Component.Page, componentId);

        var binding = component.GetModelBinding("simpleBinding");
        if (binding is null)
        {
            throw new Exception("component lookup requires the target component ");
        }
        return dataModel.GetModelData(binding);
    }
}

public class ComponentPage
{
    public string PageName { get; init; }


    public ComponentPage(string pageName)
    {
        PageName = pageName;
    }

    public void AddComponent(Component component)
    {
        ComponentDictionary[component.Id] = component;

        var parent = ComponentDictionary.Values.FirstOrDefault(c => c.ChildIds?.Any(childId => childId == component.Id) ?? false);
        if (parent is not null)
        {
            parent.Children!.Add(component);
        }
        else
        {
            Components.Add(component);
        }
    }

    public List<Component> Components { get; init; } = new();
    public Dictionary<string, Component> ComponentDictionary { get; init; } = new();
    //TODO: Run dynamics
}

public class Component
{
    public Component(string page, JsonElement element)
    {
        Id = element.GetProperty("id")!.GetString()!;
        Page = page;
        Element = element;
        if (element.TryGetProperty("type", out var type) && (type.GetString()?.Equals("group", StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            if (element.TryGetProperty("children", out var children))
            {
                Children = new List<Component>();
                ChildIds = children.EnumerateArray().Select(e => e.GetString()!).ToList();
            }
        }
    }

    public string Id { get; set; }
    public string Page { get; set; }
    public List<Component>? Children { get; set; }
    public List<string>? ChildIds { get; set; }
    public Component? Group { get; set; }
    public int? GroupIndex { get; set; }
    public JsonElement Element { get; set; }

    public Dictionary<string, string>? GetModelBindings()
    {
        if (Element.ValueKind == JsonValueKind.Object &&
             Element.TryGetProperty("dataModelBindings", out var dataModelBindings) &&
             dataModelBindings.ValueKind == JsonValueKind.Object)
        {
            return dataModelBindings
                    .EnumerateObject()
                    .Where(j => j.Value.ValueKind == JsonValueKind.String)
                    .ToDictionary(j => j.Name, j => j.Value.GetString()!);
        }
        return null;
    }
    public string? GetModelBinding(string key)
    {
        if (Element.ValueKind == JsonValueKind.Object &&
             Element.TryGetProperty("dataModelBindings", out var dataModelBindings) &&
             dataModelBindings.ValueKind == JsonValueKind.Object &&
             dataModelBindings.TryGetProperty(key, out var bindingValue))
        {
            return bindingValue.GetString();
        }

        return null;
    }
}

/// <summary>
/// Custom converter for parsing Layout files in json format to <see cref="ComponentModel" />
/// </summary>
/// <remarks>
/// The layout files in json format contains lots of polymorphism witch is hard for the
/// standard json parser to convert to an object graph. Using <see cref="Utf8JsonReader"/>
/// directly I can convert to a more suitable C# representation directly
/// </remarks>
public class ComponentModelConverter : JsonConverter<ComponentModel>
{
    /// <inheritdoc />
    public override ComponentModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        var componentModel = new ComponentModel();
        // Read dictionary of pages
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }
            var componentPage = new ComponentPage(reader.GetString()!);
            reader.Read();

            componentModel.Pages[componentPage.PageName] = ReadPage(ref reader, componentPage, options);
        }



        return componentModel;
    }

    private ComponentPage ReadPage(ref Utf8JsonReader reader, ComponentPage componentPage, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName == "data")
            {
                ReadData(ref reader, componentPage, options);
            }
            else
            {
                // Ignore other properties than "data"
                reader.Skip();
            }
        }
        return componentPage;
    }

    private void ReadData(ref Utf8JsonReader reader, ComponentPage componentPage, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            if (propertyName == "layout")
            {
                ReadLayout(ref reader, componentPage, options);
            }
            else
            {
                reader.Skip();
            }
        }
    }

    private void ReadLayout(ref Utf8JsonReader reader, ComponentPage componentPage, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            ReadComponent(ref reader, componentPage, options);
        }
    }

    private void ReadComponent(ref Utf8JsonReader reader, ComponentPage componentPage, JsonSerializerOptions options)
    {
        var component = JsonElement.ParseValue(ref reader);
        componentPage.AddComponent(
            new Component(
                page: componentPage.PageName,
                element: component
            )
        );
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ComponentModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
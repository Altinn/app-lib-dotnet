using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.App.Core.Features.Expression;
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
            var pageName = reader.GetString()!;
            reader.Read();

            componentModel.Pages[pageName] = ReadPage(ref reader, pageName, options);
        }



        return componentModel;
    }

    private PageComponent ReadPage(ref Utf8JsonReader reader, string pageName, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        PageComponent? page = null;

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
                page = ReadData(ref reader, pageName, options);
            }
            else
            {
                // Ignore other properties than "data"
                reader.Skip();
            }
        }
        if (page is null)
        {
            throw new JsonException("Missing property \"data\" on layout page");
        }
        return page;
    }

    private PageComponent ReadData(ref Utf8JsonReader reader, string pageName, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var components = new List<Component>();
        var componentLookup = new Dictionary<string, Component>();


        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString();
            reader.Read();
            switch (propertyName)
            {
                case "layout":
                    ReadLayout(ref reader, components, componentLookup, options);
                    break;
                //TODO: Add "hidden" (and "required")
                default:
                    reader.Skip();
                    break;
            }
        }

        return new PageComponent(pageName, components, componentLookup);
    }

    private void ReadLayout(ref Utf8JsonReader reader, List<Component> components, Dictionary<string, Component> componentLookup, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var component = ReadComponent(ref reader, options)!;
            // Add new component to both collections
            components.Add(component);
            AddChildrenToLookup(component, componentLookup);
        }
    }

    private static void AddChildrenToLookup(Component component, Dictionary<string, Component> componentLookup)
    {
        componentLookup[component.Id] = component;
        foreach (var child in component.Children)
        {
            AddChildrenToLookup(child, componentLookup);
        }
    }

    private Component ReadComponent(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        string? id = null;
        string? type = null;
        Dictionary<string, string>? dataModelBindings = null;
        List<string>? childIds = null;
        int maxCount = 1; // > 1 is repeating, but might not be specified for non-repeating groups
        LayoutExpression? hidden = null;
        LayoutExpression? required = null;


        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); // Not possiblie?
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            switch (propertyName.ToLowerInvariant())
            {
                case "id":
                    id = reader.GetString();
                    break;
                case "type":
                    type = reader.GetString();
                    break;
                case "datamodelbindings":
                    dataModelBindings = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                    break;
                case "children":
                    childIds = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                    break;
                case "maxcount":
                    maxCount = reader.GetInt32();
                    break;
                case "hidden":
                    hidden = JsonSerializer.Deserialize<LayoutExpression>(ref reader, options);
                    break;
                case "required":
                    required = JsonSerializer.Deserialize<LayoutExpression>(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    // Ignore unknown properties. They only have meaning in frontend
                    break;
            }
        }
        if (id is null)
        {
            throw new JsonException("\"id\" property of component should not be null");
        }
        if (type is null)
        {
            throw new JsonException("\"type\" property of component should not be null");
        }

        if (type.ToLowerInvariant() == "group")
        {
            if (childIds is null)
            {
                throw new JsonException("Component with \"type\": \"Group\" requires a \"children\" property");
            }
            var children = ReadChildren(ref reader, id, childIds, options);
            if (maxCount > 1)
            {
                return new RepeatingGroupComponent(id, type, dataModelBindings, children, maxCount, hidden, required);
            }
            else
            {
                return new GroupComponent(id, type, dataModelBindings, children, hidden, required);
            }
        }
        return new Component(id, type, dataModelBindings, children: null, hidden, required);
    }

    private List<Component> ReadChildren(ref Utf8JsonReader reader, string parentId, List<string> childIds, JsonSerializerOptions options)
    {
        var ret = new List<Component>();
        foreach (var childId in childIds)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid Group component \"{parentId}\". No components found after group component");
            }
            var component = ReadComponent(ref reader, options)!;
            if (component.Id != childId)
            {
                throw new JsonException($"Invalid Group component \"{parentId}\". Found \"{component.Id}\" instead of \"{childId}\"");
            }
            ret.Add(component);
        }
        return ret;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ComponentModel value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
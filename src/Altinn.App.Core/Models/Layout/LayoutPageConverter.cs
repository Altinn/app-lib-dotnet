using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Helpers.Extensions;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Expressions;
using System.Runtime.CompilerServices;

namespace Altinn.App.Core.Models.Layout;
/// <summary>
/// Custom converter for parsing Layout files in json format to <see cref="LayoutModel" />
/// </summary>
/// <remarks>
/// The layout files in json format contains lots of polymorphism witch is hard for the
/// standard json parser to convert to an object graph. Using <see cref="Utf8JsonReader"/>
/// directly I can convert to a more suitable C# representation directly
/// </remarks>
public class PageComponentConverter : JsonConverter<PageComponent>
{
    private static ConditionalWeakTable<JsonSerializerOptions, string> PageNames = new();
    /// <summary>
    /// Add pageName as metadata on JsonSerializerOptions that will be used for deserialization
    /// </summary>
    /// <remarks>
    /// JsonSerializer does not support passing additional arguments for deserialization, and we
    /// need the pageName to be part of the PageCompoenent at construction time to ensure nullability rules
    ///
    /// This uses a ConditionalWeakTable to attach the pageName to the options object, in a way that does not leak memory
    /// </remarks>
    public static void AddPageName(JsonSerializerOptions options, string pageName)
    {
        PageNames.Add(options, pageName);
    }


    /// <inheritdoc />
    public override PageComponent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Try to get pagename from metadata in this.AddPageName
        var pageName = PageNames.TryGetValue(options, out var outPageName) ? outPageName : "UnknownPageName";

        return ReadNotNull(ref reader, pageName, options);
    }
    /// <summary>
    /// Similar to read, but not nullable, and no pageName hack.
    /// </summary>
    public PageComponent ReadNotNull(ref Utf8JsonReader reader, string pageName, JsonSerializerOptions options)
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

        var components = new List<BaseComponent>();
        var componentLookup = new Dictionary<string, BaseComponent>();

        // Hidden is the only property that cascades.
        Expression? hidden = null;
        Expression? required = null;
        Expression? readOnly = null;

        // extra properties that are not stored in a specific class.
        Dictionary<string, string> additionalProperties = new();


        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException(); //Think this is impossible. After a JsonTokenType.StartObject, everything should be JsonTokenType.PropertyName
            }

            var propertyName = reader.GetString()!;
            reader.Read();
            switch (propertyName.ToLowerInvariant())
            {
                case "layout":
                    ReadLayout(ref reader, components, componentLookup, options);
                    break;
                case "hidden":
                    hidden = ExpressionConverter.ReadNotNull(ref reader, options);
                    break;
                case "required":
                    required = ExpressionConverter.ReadNotNull(ref reader, options);
                    break;
                case "readonly":
                    readOnly = ExpressionConverter.ReadNotNull(ref reader, options);
                    break;
                default:
                    // read extra properties
                    additionalProperties[propertyName] = reader.SkipReturnString();
                    break;
            }
        }

        return new PageComponent(pageName, components, componentLookup, hidden, required, readOnly, additionalProperties);
    }

    private void ReadLayout(ref Utf8JsonReader reader, List<BaseComponent> components, Dictionary<string, BaseComponent> componentLookup, JsonSerializerOptions options)
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

    private static void AddChildrenToLookup(BaseComponent component, Dictionary<string, BaseComponent> componentLookup)
    {
        if (componentLookup.ContainsKey(component.Id))
        {
            throw new JsonException($"Duplicate key \"{component.Id}\" detected on page \"{component.PageId}\"");
        }
        componentLookup[component.Id] = component;
        if (component is GroupComponent groupComponent)
        {
            foreach (var child in groupComponent.Children)
            {
                AddChildrenToLookup(child, componentLookup);
            }
        }
    }

    private BaseComponent ReadComponent(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        string? id = null;
        string? type = null;
        Dictionary<string, string>? dataModelBindings = null;
        Expression? hidden = null;
        Expression? required = null;
        Expression? readOnly = null;
        // Custom properities for group
        List<string>? childIds = null;
        int maxCount = 1; // > 1 is repeating, but might not be specified for non-repeating groups
        // Custom properties for Summary
        string? componentRef = null;
        string? pageRef = null;
        // Custom properties for components with optionId or literal options
        string? optionId = null;
        List<AppOption>? literalOptions = null;
        bool secure = false;

        // extra properties that are not stored in a specific class.
        Dictionary<string, string> additionalProperties = new();



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
                    // TODO: deserialize directly to make LineNumber and BytePositionInLine to give better errors
                    dataModelBindings = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
                    break;
                // case "textresourcebindings":
                //     break;
                case "children":
                    childIds = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                    break;
                case "maxcount":
                    maxCount = reader.GetInt32();
                    break;
                case "hidden":
                    hidden = ExpressionConverter.ReadNotNull(ref reader, options);
                    break;
                case "required":
                    required = ExpressionConverter.ReadNotNull(ref reader, options);
                    break;
                case "readonly":
                    readOnly = ExpressionConverter.ReadNotNull(ref reader, options);
                    break;
                // summary
                case "componentref":
                    componentRef = reader.GetString();
                    break;
                case "pageref":
                    pageRef = reader.GetString();
                    break;
                // option
                case "optionid":
                    optionId = reader.GetString();
                    break;
                case "options":
                    literalOptions = JsonSerializer.Deserialize<List<AppOption>>(ref reader, options);
                    break;
                case "secure":
                    secure = reader.TokenType == JsonTokenType.True;
                    break;
                default:
                    // store extra fields as json
                    additionalProperties[propertyName] = reader.SkipReturnString();
                    break;
            }
        }
        ThrowJsonExceptionIfNull(id);
        ThrowJsonExceptionIfNull(type);

        switch (type.ToLowerInvariant())
        {
            case "group":
                return CreateGroup(ref reader, options, id, type, dataModelBindings, hidden, required, readOnly, childIds, maxCount, additionalProperties);
            case "summary":
                return CreateSummary(id, type, hidden, componentRef, pageRef, additionalProperties);
            case "checkboxes":
            case "radiobuttons":
            case "dropdown":
                return CreateOption(id, type, dataModelBindings, hidden, required, readOnly, optionId, literalOptions, secure, additionalProperties);
        }

        // Most compoents are handled as BaseComponent
        return new BaseComponent(id, type, dataModelBindings, hidden, required, readOnly, additionalProperties);
    }

    private BaseComponent CreateGroup(ref Utf8JsonReader reader, JsonSerializerOptions options, string id, string type, Dictionary<string, string>? dataModelBindings, Expression? hidden, Expression? required, Expression? readOnly, List<string>? childIds, int maxCount, Dictionary<string, string> additionalProperties)
    {
        if (childIds is null)
        {
            throw new JsonException("Component with \"type\": \"Group\" requires a \"children\" property");
        }
        var children = ReadChildren(ref reader, id, childIds, options);
        if (maxCount > 1)
        {
            if (!(dataModelBindings?.ContainsKey("group") ?? false))
            {
                throw new JsonException($"A group id:\"{id}\" with maxCount: {maxCount} does not have a \"group\" dataModelBinding");
            }

            return new RepeatingGroupComponent(id, type, dataModelBindings, children, maxCount, hidden, required, readOnly, additionalProperties);
        }
        else
        {
            return new GroupComponent(id, type, dataModelBindings, children, hidden, required, readOnly, additionalProperties);
        }
    }

    private static BaseComponent CreateSummary(string id, string type, Expression? hidden, string? componentRef, string? pageRef, Dictionary<string, string> additionalProperties)
    {
        if (componentRef is null || pageRef is null)
        {
            throw new JsonException("Component with \"type\": \"Summary\" requires \"componentRef\" and \"pageRef\" properties");
        }

        return new SummaryComponent(id, type, hidden, componentRef, pageRef, additionalProperties);
    }

    private static BaseComponent CreateOption(string id, string type, Dictionary<string, string>? dataModelBindings, Expression? hidden, Expression? required, Expression? readOnly, string? optionId, List<AppOption>? literalOptions, bool secure, Dictionary<string, string> additionalProperties)
    {
        if (optionId is null && literalOptions is null)
        {
            throw new JsonException("\"optionId\" or \"options\" is required on checkboxes, radiobuttons and dropdowns");
        }
        if (optionId is not null && literalOptions is not null)
        {
            throw new JsonException("\"optionId\" and \"options\" can't both be specified");
        }
        if (literalOptions is not null && secure)
        {
            throw new JsonException("\"secure\": true is invalid for components with literal \"options\"");
        }

        return new OptionsComponent(id, type, dataModelBindings, hidden, required, readOnly, optionId, literalOptions, secure, additionalProperties);
    }


    /// <summary>
    /// Utility method to recduce so called Coginitve Complexity by writing if in the meth
    /// </summary>
    private static void ThrowJsonExceptionIfNull([System.Diagnostics.CodeAnalysis.NotNull] object? obj, [CallerArgumentExpression("obj")]string? propertyName = null)
    {
        if (obj is null)
        {
            throw new JsonException($"\"{propertyName}\" property of component should not be null");
        }
    }


    private List<BaseComponent> ReadChildren(ref Utf8JsonReader reader, string parentId, List<string> childIds, JsonSerializerOptions options)
    {
        var ret = new List<BaseComponent>();
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
                throw new JsonException($"Invalid Group component \"{parentId}\". The next component has id \"{component.Id}\" instead of \"{childId}\"");
            }
            ret.Add(component);
        }
        return ret;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PageComponent value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
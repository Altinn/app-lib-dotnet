using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Helpers.Extensions;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Core.Models.Expressions;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;


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
    private static readonly AsyncLocal<string?> PageName = new();
    /// <summary>
    /// Store pageName to be used for deserialization
    /// </summary>
    /// <remarks>
    /// JsonSerializer does not support passing additional arguments for deserialization, and we
    /// need the pageName to be part of the PageCompoenent at construction time to ensure nullability rules
    ///
    /// This uses a AsyncLocal to pass the pageName as an additional parameter
    /// </remarks>
    public static void SetAsyncLocalPageName(string pageName)
    {
        PageName.Value = pageName;
    }


    /// <inheritdoc />
    public override PageComponent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Try to get pagename from metadata in this.AddPageName
        var pageName = PageName.Value ?? "UnknownPageName";
        PageName.Value = null;

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
        var childToGroupMapping = new Dictionary<string, (GroupComponent, int)>();

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
                    ReadLayout(ref reader, components, componentLookup, childToGroupMapping, options);
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

        var page = new PageComponent(pageName, components, componentLookup, hidden, required, readOnly, additionalProperties);
        ValidateGroupChildren(page);
        return page;
    }

    private void ReadLayout(ref Utf8JsonReader reader, List<BaseComponent> components, Dictionary<string, BaseComponent> componentLookup, Dictionary<string, (GroupComponent, int)> childToGroupMapping, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var component = ReadComponent(ref reader, childToGroupMapping, options)!;

            // Add to component lookup
            AddChildrenToLookup(component, componentLookup);

            // Check if component should be added to group children or to page
            if (childToGroupMapping.ContainsKey(component.Id))
            {
                var (parent, index) = childToGroupMapping[component.Id];
                var children = (List<BaseComponent>)parent.Children;
                while (children.Count <= index)
                {
                    children.Add(null!);
                }
                children[index] = component;
                component.Parent = parent;
            }
            else
            {
                components.Add(component);
            }
        }
    }

    private static void AddChildrenToLookup(BaseComponent component, Dictionary<string, BaseComponent> componentLookup)
    {
        if (componentLookup.ContainsKey(component.Id))
        {
            throw new JsonException($"Duplicate key \"{component.Id}\" detected on page \"{component.PageId}\"");
        }
        componentLookup[component.Id] = component;
    }

    private static readonly Regex MultiPageIndexRegex = new Regex(@"^(\d+:)?([^\s:]+)$");
    private static void AddChildrenToMapping(GroupComponent component, List<string> children, Dictionary<string, (GroupComponent, int)> childToGroupMapping)
    {
        for (var index = 0; index < children.Count; index++)
        {
            // Remove MultiPageIndex if present
            var match = MultiPageIndexRegex.Match(children[index]);
            var childId = match.Groups[2].Value;
            if (childToGroupMapping.ContainsKey(childId))
            {
                throw new JsonException($"Component \"{component.Id}\" tried to claim \"{childId}\" as a child, but that child is already claimed by \"{childToGroupMapping[childId].Item1.Id}\"");
            }
            childToGroupMapping[childId] = (component, index);
        }
    }

    private BaseComponent ReadComponent(ref Utf8JsonReader reader, Dictionary<string, (GroupComponent, int)> childToGroupMapping, JsonSerializerOptions options)
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
        List<string>? children = null;
        int maxCount = 1; // > 1 is repeating, but might not be specified for non-repeating groups
        // Custom properties for Summary
        string? componentRef = null;
        string? pageRef = null;
        // Custom properties for components with optionId or literal options
        string? optionId = null;
        List<AppOption>? literalOptions = null;
        OptionsSource? optionsSource = null;
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
                    children = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                    break;
                case "rows":
                    children = GridConfig.ReadGridChildren(ref reader, options);
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
                case "optionsid":
                    optionId = reader.GetString();
                    break;
                case "options":
                    literalOptions = JsonSerializer.Deserialize<List<AppOption>>(ref reader, options);
                    break;
                case "source":
                    optionsSource = JsonSerializer.Deserialize<OptionsSource>(ref reader, options);
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
                ThrowJsonExceptionIfNull(children, "Component with \"type\": \"Group\" requires a \"children\" property");

                if (maxCount > 1)
                {
                    if (!(dataModelBindings?.ContainsKey("group") ?? false))
                    {
                        throw new JsonException($"A group id:\"{id}\" with maxCount: {maxCount} does not have a \"group\" dataModelBinding");
                    }

                    var repComponent = new RepeatingGroupComponent(id, type, dataModelBindings, new List<BaseComponent>(), maxCount, hidden, required, readOnly, additionalProperties);
                    AddChildrenToMapping(repComponent, children, childToGroupMapping);
                    return repComponent;
                }
                else
                {
                    var groupComponent = new GroupComponent(id, type, dataModelBindings, new List<BaseComponent>(), hidden, required, readOnly, additionalProperties);
                    AddChildrenToMapping(groupComponent, children, childToGroupMapping);
                    return groupComponent;
                }
            case "grid":
                var gridComponent = new GridComponent(id, type, dataModelBindings, new List<BaseComponent>(), hidden, required, readOnly, additionalProperties);
                AddChildrenToMapping(gridComponent, children!, childToGroupMapping);
                return gridComponent;
            case "summary":
                ValidateSummary(componentRef, pageRef);
                return new SummaryComponent(id, type, hidden, componentRef, pageRef, additionalProperties);
            case "checkboxes":
            case "radiobuttons":
            case "dropdown":
                ValidateOptions(optionId, literalOptions, optionsSource, secure);
                return new OptionsComponent(id, type, dataModelBindings, hidden, required, readOnly, optionId, literalOptions, optionsSource, secure, additionalProperties);
        }

        // Most compoents are handled as BaseComponent
        return new BaseComponent(id, type, dataModelBindings, hidden, required, readOnly, additionalProperties);
    }

    private static void ValidateOptions(string? optionId, List<AppOption>? literalOptions, OptionsSource? optionsSource, bool secure)
    {
        if (optionId is null && literalOptions is null && optionsSource is null)
        {
            throw new JsonException("\"optionId\" or \"options\" or \"source\" is required on checkboxes, radiobuttons and dropdowns");
        }
        if (optionId is not null && literalOptions is not null)
        {
            throw new JsonException("\"optionId\" and \"options\" can't both be specified");
        }
        if (optionId is not null && optionsSource is not null)
        {
            throw new JsonException("\"optionId\" and \"source\" can't both be specified");
        }
        if (optionsSource is not null && literalOptions is not null)
        {
            throw new JsonException("\"source\" and \"options\" can't both be specified");
        }
        if (literalOptions is not null && secure)
        {
            throw new JsonException("\"secure\": true is invalid for components with literal \"options\"");
        }
        if (optionsSource is not null && secure)
        {
            throw new JsonException("\"secure\": true is invalid for components that reference a repeating group \"source\"");
        }
    }

    private static void ValidateSummary([NotNull] string? componentRef, [NotNull] string? pageRef)
    {
        if (componentRef is null || pageRef is null)
        {
            throw new JsonException("Component with \"type\": \"Summary\" requires \"componentRef\" and \"pageRef\" properties");
        }
    }

    private static void ValidateGroupChildren(GroupComponent groupComponent)
    {
        var children = (List<BaseComponent>)groupComponent.Children;
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is null)
            {
                throw new JsonException($"Group \"{groupComponent.Id}\" has a null child at index {i}");
            }
            if (children[i] is GroupComponent childGroup)
            {
                ValidateGroupChildren(childGroup);
            }
        }
    }

    /// <summary>
    /// Utility method to recduce so called Coginitve Complexity by writing if in the meth
    /// </summary>
    private static void ThrowJsonExceptionIfNull([NotNull] object? obj, string? message = null, [CallerArgumentExpression("obj")] string? propertyName = null)
    {
        if (obj is null)
        {
            throw new JsonException(message ?? $"\"{propertyName}\" property of component should not be null");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, PageComponent value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

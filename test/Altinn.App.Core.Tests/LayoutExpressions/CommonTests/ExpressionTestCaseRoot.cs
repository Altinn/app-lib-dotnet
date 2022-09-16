#nullable enable

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Altinn.App.Core.Configuration;
using Altinn.App.Core.Implementation.Expression;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Tests.LayoutExpressions;

public class ExpressionTestCaseRoot
{
    [JsonIgnore]
    public string? Filename { get; set; }

    [JsonIgnore]
    public string? FullPath { get; set; }

    [JsonIgnore]
    public string? Folder { get; set; }

    [JsonIgnore]
    public string? RawJson { get; set; }

    [JsonIgnore]
    public Exception? ParsingException { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("expression")]
    public LayoutExpression Expression { get; set; } = default!;

    [JsonPropertyName("context")]
    public ComponentContextForTestSpec? Context { get; set; } = default!;

    [JsonPropertyName("expects")]
    public JsonElement Expects { get; set; } = default!;

    [JsonPropertyName("expectsFailure")]
    public string? ExpectsFailure { get; set; }

    [JsonPropertyName("layouts")]
    public ComponentModel ComponentModel { get; set; } = default!;

    [JsonPropertyName("dataModel")]
    public JsonElement? DataModel { get; set; }

    [JsonPropertyName("frontendSettings")]
    public FrontEndSettings? FrontEndSettings { get; set; }

    [JsonPropertyName("instanceContext")]
    [JsonConverter(typeof(InstanceConverter))]
    public Instance? InstanceContext { get; set; }

    public override string ToString()
    {
        return $"{Filename}: {Name}";
    }
}

public class InstanceConverter : JsonConverter<Instance>
{
    public override Instance? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var testInstance = JsonSerializer.Deserialize<InstanceForTestSpec>(ref reader, options);
        if (testInstance is null)
        {
            return null;
        }

        return new Instance
        {
            AppId = testInstance.AppId,
            Id = testInstance.InstanceId,
            InstanceOwner = new()
            {
                PartyId = testInstance.InstanceOwnerPartyId,
            }
        };
    }

    public override void Write(Utf8JsonWriter writer, Instance value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public class InstanceForTestSpec
    {
        [JsonPropertyName("instanceId")]
        public string InstanceId { get; set; } = default!;

        [JsonPropertyName("appId")]
        public string AppId { get; set; } = default!;

        [JsonPropertyName("instanceOwnerPartyId")]
        public string InstanceOwnerPartyId { get; set; } = default!;
    }
}

public class ComponentContextForTestSpec
{
    [JsonPropertyName("component")]
    public string ComponentId { get; set; } = default!;

    [JsonPropertyName("rowIndices")]
    public int[]? RowIndices { get; set; }

    [JsonPropertyName("currentLayout")]
    public string CurrentPageName { get; set; } = default!;

    public ComponentContext ToContext(ComponentModel model)
    {
        return new ComponentContext(model.GetComponent(CurrentPageName, ComponentId), RowIndices);
    }
}
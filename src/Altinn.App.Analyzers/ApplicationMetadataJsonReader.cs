using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Altinn.App.Analyzers;

internal readonly record struct ParsedJsonValue<T>(T Value, IJsonLineInfo LineInfo);

internal sealed record ApplicationMetadata(
    ParsedJsonValue<IReadOnlyList<DataTypeInfo>> DataTypes,
    OnEntryInfo? OnEntry = null
);

internal sealed record DataTypeInfo(ParsedJsonValue<string> Id, ParsedJsonValue<AppLogicInfo>? AppLogic);

internal sealed record OnEntryInfo(ParsedJsonValue<string> Show);

internal sealed record AppLogicInfo(ParsedJsonValue<string> ClassRef);

internal static class ApplicationMetadataJsonReader
{
    public static ApplicationMetadata Read(string json)
    {
        var dataTypeInfo = new List<DataTypeInfo>();

        using var reader = new JsonTextReader(new StringReader(json));

        JsonLoadSettings loadSettings = new JsonLoadSettings()
        {
            CommentHandling = CommentHandling.Ignore,
            LineInfoHandling = LineInfoHandling.Load
        };

        var metadata = JObject.Load(reader, loadSettings);
        var dataTypesArr = metadata.GetValue("dataTypes") as JArray;
        if (dataTypesArr is null)
            throw new JsonException("Failed to parse 'dataTypes'");
        var dataTypes = dataTypesArr.ReadWithLineInfo((IReadOnlyList<DataTypeInfo>)dataTypeInfo, "dataTypes");

        foreach (var dataTypeToken in dataTypesArr)
        {
            var dataType = (dataTypeToken as JObject) ?? throw new JsonException("Failed to parse 'dataType'");

            var dataTypeId = dataType.GetStringValue("id");

            var appLogicObj = dataType.GetValue("appLogic") as JObject;
            if (appLogicObj is null)
            {
                dataTypeInfo.Add(new DataTypeInfo(dataTypeId, AppLogic: null));
                continue;
            }

            var classRef = appLogicObj.GetStringValue("classRef");
            var appLogic = appLogicObj.ReadWithLineInfo(new AppLogicInfo(classRef), "appLogic");
            dataTypeInfo.Add(new DataTypeInfo(dataTypeId, appLogic));
        }

        var onEntryObj = metadata.GetValue("onEntry") as JObject;
        OnEntryInfo? onEntry = null;
        if (onEntryObj is not null)
        {
            var show = onEntryObj.GetStringValue("show");
            onEntry = new OnEntryInfo(show);
        }

        return new ApplicationMetadata(dataTypes, onEntry);
    }

    private static ParsedJsonValue<string> GetStringValue(this JObject obj, string propertyName)
    {
        var token = obj.GetValue(propertyName) ?? throw new JsonException($"Failed to parse '{propertyName}'");
        var jvalue = (token as JValue) ?? throw new JsonException($"Failed to parse '{propertyName}'");
        var value = (jvalue.Value as string) ?? throw new JsonException($"Failed to parse '{propertyName}'");

        return jvalue.ReadWithLineInfo(value, propertyName);
    }

    private static ParsedJsonValue<T> ReadWithLineInfo<T>(this JToken token, T value, string propertyName)
    {
        if (token is not IJsonLineInfo lineInfo || !lineInfo.HasLineInfo())
            throw new JsonException($"Could not read line info while parsing '{propertyName}'");

        return new ParsedJsonValue<T>(value, lineInfo);
    }
}

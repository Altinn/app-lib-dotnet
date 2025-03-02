using Altinn.App.Analyzers.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Altinn.App.Analyzers.ApplicationMetadata;

internal sealed record ApplicationMetadataInfo(
    ParsedJsonToken<IReadOnlyList<DataTypeInfo>> DataTypes,
    ParsedJsonToken<OnEntryInfo?> OnEntry
);

internal abstract record ApplicationMetadataParseResult
{
    private ApplicationMetadataParseResult() { }

    internal sealed record Ok(ApplicationMetadataInfo Value) : ApplicationMetadataParseResult;

    internal sealed record FailedToParse(JsonTokenDescriptor Token) : ApplicationMetadataParseResult;

    internal sealed record Error(Exception Exception) : ApplicationMetadataParseResult;

    internal sealed record Cancelled : ApplicationMetadataParseResult;
}

internal sealed record DataTypeInfo(ParsedJsonToken<string> Id, ParsedJsonToken<AppLogicInfo?> AppLogic);

internal sealed record OnEntryInfo(ParsedJsonToken<string> Show);

internal sealed record AppLogicInfo(ParsedJsonToken<string> ClassRef);

internal static class ApplicationMetadataJsonReader
{
    public static ApplicationMetadataParseResult Read(string json, CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return new ApplicationMetadataParseResult.Cancelled();

            var dataTypeInfo = new List<DataTypeInfo>();

            using var reader = new JsonTextReader(new StringReader(json));

            JsonLoadSettings loadSettings = new JsonLoadSettings()
            {
                CommentHandling = CommentHandling.Ignore,
                LineInfoHandling = LineInfoHandling.Load,
            };

            var metadata = JObject.Load(reader, loadSettings);

            if (!metadata.GetValue<JArray>("dataTypes").IsValid(out var dataTypes, out var dataTypesToken))
                return new ApplicationMetadataParseResult.FailedToParse(dataTypesToken);

            foreach (var dataTypeToken in dataTypesToken.EnumerateAs<JObject>())
            {
                if (cancellationToken.IsCancellationRequested)
                    return new ApplicationMetadataParseResult.Cancelled();

                var dataTypeResult = ProcessDataType(dataTypeInfo, dataTypeToken);
                if (dataTypeResult is not null)
                    return dataTypeResult;
            }

            OnEntryInfo? onEntryInfo = null;
            if (metadata.GetValue<JObject>("onEntry").IsValid(out var onEntry, out var onEntryToken))
            {
                if (!onEntry.GetValue<JValue>("show").IsValidNonNullString(out var showToken))
                    return new ApplicationMetadataParseResult.FailedToParse(showToken);

                onEntryInfo = new OnEntryInfo(showToken);
            }

            var result = new ApplicationMetadataInfo(
                dataTypesToken.Parsed<IReadOnlyList<DataTypeInfo>>(dataTypeInfo),
                onEntryToken.Parsed(onEntryInfo)
            );
            return new ApplicationMetadataParseResult.Ok(result);
        }
        catch (Exception ex)
        {
            return new ApplicationMetadataParseResult.Error(ex);
        }
    }

    private static ApplicationMetadataParseResult? ProcessDataType(
        List<DataTypeInfo> dataTypeInfo,
        JsonToken<JObject> dataTypeToken
    )
    {
        if (dataTypeToken.Value is not { } dataType)
            return new ApplicationMetadataParseResult.FailedToParse(dataTypeToken);

        if (!dataType.GetValue<JValue>("id").IsValidNonNullString(out var dataTypeIdToken))
            return new ApplicationMetadataParseResult.FailedToParse(dataTypeIdToken);

        if (!dataType.GetValue<JObject>("appLogic").IsValid(out var appLogic, out var appLogicToken))
        {
            dataTypeInfo.Add(new DataTypeInfo(dataTypeIdToken, AppLogic: appLogicToken.Parsed<AppLogicInfo?>(null)));
            return null;
        }

        if (!appLogic.GetValue<JValue>("classRef").IsValidNonNullString(out var classRefToken))
            return new ApplicationMetadataParseResult.FailedToParse(classRefToken);

        dataTypeInfo.Add(
            new DataTypeInfo(dataTypeIdToken, appLogicToken.Parsed<AppLogicInfo?>(new AppLogicInfo(classRefToken)))
        );

        return null;
    }
}

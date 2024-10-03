using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Altinn.App.Api.Controllers.Conventions;

internal class EnumAsNumberFormatter : SystemTextJsonOutputFormatter
{
    internal EnumAsNumberFormatter(string settingsName, JsonOptions options)
        : base(CreateSerializerOptions(options))
    {
        SettingsName = settingsName;
    }

    internal string SettingsName { get; }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        if (context.HttpContext.GetJsonSettingsName() != SettingsName)
        {
            return false;
        }

        return base.CanWriteResult(context);
    }

    private static JsonSerializerOptions CreateSerializerOptions(JsonOptions options)
    {
        var newOptions = new JsonSerializerOptions(options.JsonSerializerOptions);
        newOptions.Converters.Add(new JsonNumberEnumConverterFactory());
        return newOptions;
    }
}

internal class JsonNumberEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(JsonNumberEnumConverter<>).MakeGenericType(typeToConvert);
        var factoryInstance =
            Activator.CreateInstance(converterType)
            ?? throw new InvalidOperationException(
                $"Failed to create converter factory for type {converterType.FullName}"
            );
        var converterFactory = (JsonConverterFactory)factoryInstance;
        var instance =
            converterFactory.CreateConverter(typeToConvert, options)
            ?? throw new InvalidOperationException($"Failed to converter factory for type {typeToConvert.FullName}");

        return instance;
    }
}

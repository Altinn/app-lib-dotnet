using System.Text.Json;
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
        return newOptions;
    }
}

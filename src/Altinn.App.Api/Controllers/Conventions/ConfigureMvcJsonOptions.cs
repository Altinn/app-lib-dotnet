using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Controllers.Conventions;

public class ConfigureMvcJsonOptions : IConfigureOptions<MvcOptions>
{
    private readonly string _jsonSettingsName;
    private readonly IOptionsMonitor<JsonOptions> _jsonOptions;
    private readonly ILoggerFactory _loggerFactory;

    public ConfigureMvcJsonOptions(
        string jsonSettingsName,
        IOptionsMonitor<JsonOptions> jsonOptions,
        ILoggerFactory loggerFactory
    )
    {
        _jsonSettingsName = jsonSettingsName;
        _jsonOptions = jsonOptions;
        _loggerFactory = loggerFactory;
    }

    public void Configure(MvcOptions options)
    {
        var jsonOptions = _jsonOptions.Get(_jsonSettingsName);
        var logger = _loggerFactory.CreateLogger<EnumAsNumberFormatter>();
        options.OutputFormatters.Insert(0, new EnumAsNumberFormatter(_jsonSettingsName, jsonOptions));
        // options.OutputFormatters.Insert(
        //     0,
        //     new SpecificSystemTextJsonOutputFormatter(
        //         _jsonSettingsName,
        //         jsonOptions.JsonSerializerOptions));
    }
}

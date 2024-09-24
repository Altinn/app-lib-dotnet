using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Controllers.Conventions;

/// <summary>
/// Configures MVC options to use a specific JSON serialization settings for enum-to-number conversion.
/// </summary>
public class ConfigureMvcJsonOptions : IConfigureOptions<MvcOptions>
{
    private readonly string _jsonSettingsName;
    private readonly IOptionsMonitor<JsonOptions> _jsonOptions;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureMvcJsonOptions"/> class.
    /// </summary>
    /// <param name="jsonSettingsName">The name of the JSON settings to be used for enum-to-number conversion.</param>
    /// <param name="jsonOptions">An <see cref="IOptionsMonitor{TOptions}"/> to access the named JSON options.</param>
    /// <param name="loggerFactory">A factory for creating loggers.</param>
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

    /// <summary>
    /// Configures the MVC options to use the EnumAsNumberFormatter for the specified JSON settings.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/> to configure.</param>
    public void Configure(MvcOptions options)
    {
        var jsonOptions = _jsonOptions.Get(_jsonSettingsName);
        var logger = _loggerFactory.CreateLogger<EnumAsNumberFormatter>();
        options.OutputFormatters.Insert(0, new EnumAsNumberFormatter(_jsonSettingsName, jsonOptions));
    }
}

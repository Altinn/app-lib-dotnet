using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Altinn.App.Core.Constants;
using Altinn.App.Core.Internal.App;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

/// <summary>
/// Configuration properties for eFormidling in a process task. All properties support environment-specific values using 'env' attributes.
/// </summary>
public class AltinnEFormidlingConfiguration
{
    /// <summary>
    /// Controls whether eFormidling should be enabled for this task.
    /// </summary>
    [XmlElement(ElementName = "enabled", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> Enabled { get; set; } = [];

    /// <summary>
    /// The organization number of the receiver of the eFormidling message. Can be omitted.
    /// </summary>
    [XmlElement(ElementName = "receiver", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> Receiver { get; set; } = [];

    /// <summary>
    /// The process identifier for the eFormidling message.
    /// </summary>
    [XmlElement(ElementName = "process", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> Process { get; set; } = [];

    /// <summary>
    /// The standard identifier for the document.
    /// </summary>
    [XmlElement(ElementName = "standard", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> Standard { get; set; } = [];

    /// <summary>
    /// The type version of the document.
    /// </summary>
    [XmlElement(ElementName = "typeVersion", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> TypeVersion { get; set; } = [];

    /// <summary>
    /// The type of the document.
    /// </summary>
    [XmlElement(ElementName = "type", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> Type { get; set; } = [];

    /// <summary>
    /// The security level for the eFormidling message.
    /// </summary>
    [XmlElement(ElementName = "securityLevel", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> SecurityLevel { get; set; } = [];

    /// <summary>
    /// Optional DPF shipment type for the eFormidling message.
    /// </summary>
    [XmlElement(ElementName = "dpfShipmentType", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> DpfShipmentType { get; set; } = [];

    /// <summary>
    /// List of data type IDs to include in the eFormidling shipment.
    /// </summary>
    [XmlElement(ElementName = "dataTypes", Namespace = "http://altinn.no/process")]
    public List<AltinnEFormidlingDataTypesConfig> DataTypes { get; set; } = [];

    internal ValidAltinnEFormidlingConfiguration Validate(HostingEnvironment env)
    {
        List<string>? errorMessages = null;

        string? receiver = GetConfigValue(Receiver, env);

        string? process = GetConfigValue(Process, env);
        if (process.IsNullOrWhitespace(ref errorMessages, $"No Process configuration found for environment {env}"))
            ThrowApplicationConfigException(errorMessages);

        string? standard = GetConfigValue(Standard, env);
        if (standard.IsNullOrWhitespace(ref errorMessages, $"No Standard configuration found for environment {env}"))
            ThrowApplicationConfigException(errorMessages);

        string? typeVersion = GetConfigValue(TypeVersion, env);
        if (
            typeVersion.IsNullOrWhitespace(
                ref errorMessages,
                $"No TypeVersion configuration found for environment {env}"
            )
        )
            ThrowApplicationConfigException(errorMessages);

        string? type = GetConfigValue(Type, env);
        if (type.IsNullOrWhitespace(ref errorMessages, $"No Type configuration found for environment {env}"))
            ThrowApplicationConfigException(errorMessages);

        string? securityLevelValue = GetConfigValue(SecurityLevel, env);
        if (
            securityLevelValue.IsNullOrWhitespace(
                ref errorMessages,
                $"No SecurityLevel configuration found for environment {env}"
            )
        )
            ThrowApplicationConfigException(errorMessages);

        if (!int.TryParse(securityLevelValue, out int securityLevel))
        {
            errorMessages ??= new List<string>(1);
            errorMessages.Add($"SecurityLevel must be a valid integer for environment {env}");
            ThrowApplicationConfigException(errorMessages);
        }

        string? dpfShipmentType = GetConfigValue(DpfShipmentType, env);

        List<string> dataTypes = GetDataTypesForEnvironment(env);

        return new ValidAltinnEFormidlingConfiguration(
            receiver,
            process,
            standard,
            typeVersion,
            type,
            securityLevel,
            dpfShipmentType,
            dataTypes
        );
    }

    [DoesNotReturn]
    private static void ThrowApplicationConfigException(List<string> errorMessages)
    {
        throw new ApplicationConfigException(
            "eFormidling process task configuration is not valid: " + string.Join(",\n", errorMessages)
        );
    }

    private static string? GetConfigValue(List<AltinnEnvironmentConfig> configs, HostingEnvironment env)
    {
        AltinnEnvironmentConfig? config = AltinnTaskExtension.GetConfigForEnvironment(env, configs);
        return config?.Value;
    }

    /// <summary>
    /// Gets the data type IDs for the specified environment.
    /// Returns environment-specific configuration if available, otherwise returns global configuration.
    /// </summary>
    private List<string> GetDataTypesForEnvironment(HostingEnvironment env)
    {
        if (DataTypes.Count == 0)
            return [];

        const string globalKey = "__global__";
        Dictionary<string, List<string>> lookup = new();

        foreach (var dataTypesConfig in DataTypes)
        {
            var key = string.IsNullOrWhiteSpace(dataTypesConfig.Environment)
                ? globalKey
                : AltinnEnvironments.GetHostingEnvironment(dataTypesConfig.Environment).ToString();

            if (lookup.TryGetValue(key, out var existingList))
            {
                existingList.AddRange(dataTypesConfig.DataTypeIds);
            }
            else
            {
                lookup[key] = new List<string>(dataTypesConfig.DataTypeIds);
            }
        }

        return lookup.GetValueOrDefault(env.ToString()) ?? lookup.GetValueOrDefault(globalKey) ?? [];
    }
}

/// <summary>
/// Configuration for data types in eFormidling with environment support
/// </summary>
public class AltinnEFormidlingDataTypesConfig
{
    /// <summary>
    /// The environment this configuration applies to. If omitted, applies to all environments.
    /// </summary>
    [XmlAttribute("env")]
    public string? Environment { get; set; }

    /// <summary>
    /// List of data type IDs to include in eFormidling for this environment.
    /// </summary>
    [XmlElement(ElementName = "dataType", Namespace = "http://altinn.no/process")]
    public List<string> DataTypeIds { get; set; } = [];
}

/// <summary>
/// Validated eFormidling configuration with all required fields guaranteed to be non-null
/// </summary>
/// <param name="Receiver">The organisation number of the receiver. Only Norwegian organisations supported. (Can be omitted)</param>
/// <param name="Process">The process identifier for the eFormidling message</param>
/// <param name="Standard">The standard identifier for the document</param>
/// <param name="TypeVersion">The type version of the document</param>
/// <param name="Type">The type of the document</param>
/// <param name="SecurityLevel">The security level for the eFormidling message</param>
/// <param name="DpfShipmentType">Optional DPF shipment type for the eFormidling message</param>
/// <param name="DataTypes">List of data type IDs to include in the eFormidling shipment</param>
public readonly record struct ValidAltinnEFormidlingConfiguration(
    string? Receiver,
    string Process,
    string Standard,
    string TypeVersion,
    string Type,
    int SecurityLevel,
    string? DpfShipmentType,
    List<string> DataTypes
);

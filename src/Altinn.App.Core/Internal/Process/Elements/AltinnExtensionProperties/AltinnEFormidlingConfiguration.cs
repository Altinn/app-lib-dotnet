using System.Xml.Serialization;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

/// <summary>
/// Configuration properties for eFormidling in a process task
/// </summary>
public class AltinnEFormidlingConfiguration
{
    /// <summary>
    /// Controls whether eFormidling should be enabled for this task.
    /// Supports environment-specific configuration through the 'env' attribute.
    /// If no environment is specified, the value applies to all environments.
    /// Environment-specific values take precedence over global values.
    /// </summary>
    [XmlElement(ElementName = "enabled", Namespace = "http://altinn.no/process")]
    public List<AltinnEnvironmentConfig> Enabled { get; set; } = [];
}

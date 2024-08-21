using System.Xml.Serialization;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

/// <summary>
/// Configuration properties for PDF in a process task
/// </summary>
public class AltinnPdfConfiguration
{
    /// <summary>
    /// Define what task IDs that should be included in the PDF.
    /// </summary>
    [XmlArray(ElementName = "tasksToIncludeInPdf", Namespace = "http://altinn.no/process", IsNullable = true)]
    [XmlArrayItem(ElementName = "taskId", Namespace = "http://altinn.no/process")]
    public List<string> TaskIds { get; set; } = [];

    /// <summary>
    /// Set the filename of the PDF. Supports text resource keys for language support.
    /// </summary>
    [XmlElement("filename", Namespace = "http://altinn.no/process")]
    public string? Filename { get; set; }
}

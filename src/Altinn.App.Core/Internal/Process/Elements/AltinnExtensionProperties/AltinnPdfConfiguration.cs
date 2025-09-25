using System.Xml.Serialization;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

/// <summary>
/// Configuration properties for PDF in a process task
/// </summary>
public class AltinnPdfConfiguration
{
    /// <summary>
    /// Set the data type to use when storing the PDF. If not set, ref-data-as-pdf will be used.
    /// </summary>
    [XmlElement("dataTypeId", Namespace = "http://altinn.no/process")]
    public string? DataTypeId { get; set; }

    /// <summary>
    /// Set the filename of the PDF. Supports text resource keys for language support.
    /// </summary>
    [XmlElement("filename", Namespace = "http://altinn.no/process")]
    public string? Filename { get; set; }

    internal ValidAltinnPdfConfiguration Validate()
    {
        return new ValidAltinnPdfConfiguration(DataTypeId, Filename);
    }
}

internal readonly record struct ValidAltinnPdfConfiguration(string? DataTypeId, string? Filename);

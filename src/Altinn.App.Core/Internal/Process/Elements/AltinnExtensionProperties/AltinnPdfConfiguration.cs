using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Altinn.App.Core.Internal.App;

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
    [XmlArrayItem(ElementName = "task", Namespace = "http://altinn.no/process")]
    public List<string> TaskIds { get; set; } = [];

    /// <summary>
    /// Set the filename of the PDF. Supports text resource keys for language support.
    /// </summary>
    [XmlElement("filename", Namespace = "http://altinn.no/process")]
    public string? Filename { get; set; }

    internal ValidAltinnPdfConfiguration Validate()
    {
        List<string>? errorMessages = null;

        List<string> taskIds = TaskIds;
        string? filename = Filename;

        if (
            taskIds.IsEmpty(
                ref errorMessages,
                "No TaskIds to render in PDF have been configured. Add at least one taskId."
            )
        )
            ThrowApplicationConfigException(errorMessages);

        return new ValidAltinnPdfConfiguration(taskIds, filename);
    }

    [DoesNotReturn]
    private static void ThrowApplicationConfigException(List<string> errorMessages)
    {
        throw new ApplicationConfigException(
            "Pdf process task configuration is not valid: " + string.Join(",\n", errorMessages)
        );
    }
}

internal readonly record struct ValidAltinnPdfConfiguration(List<string> TaskIds, string? Filename);

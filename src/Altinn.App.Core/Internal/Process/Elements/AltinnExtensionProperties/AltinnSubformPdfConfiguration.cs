using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Altinn.App.Core.Internal.App;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

/// <summary>
/// Configuration properties for subform PDF generation in a process task
/// </summary>
public class AltinnSubformPdfConfiguration
{
    /// <summary>
    /// Set the filename of the PDF using a text resource key.
    /// </summary>
    [XmlElement("filenameTextResourceKey", Namespace = "http://altinn.no/process")]
    public string? FilenameTextResourceKey { get; set; }

    /// <summary>
    /// The ID of the subform component to render in the PDFs.
    /// </summary>
    [XmlElement("subformComponentId", Namespace = "http://altinn.no/process")]
    public string? SubformComponentId { get; set; }

    /// <summary>
    /// The ID of the data type associated with the subform component.
    /// </summary>
    [XmlElement("subformDataTypeId", Namespace = "http://altinn.no/process")]
    public string? SubformDataTypeId { get; set; }

    /// <summary>
    /// The maximum degree of parallelism when generating PDFs for multiple subform data elements.
    /// Defaults to 1 (sequential) to avoid overwhelming the PDF microservice.
    /// Set to a higher value (e.g., 4-8) only if your PDF microservice can handle concurrent requests.
    /// Must be at least 1.
    /// </summary>
    [XmlElement("degreeOfParallelism", Namespace = "http://altinn.no/process")]
    public int DegreeOfParallelism { get; set; } = 1;

    internal ValidAltinnSubformPdfConfiguration Validate()
    {
        List<string>? errorMessages = null;

        string? normalizedFilename = string.IsNullOrWhiteSpace(FilenameTextResourceKey)
            ? null
            : FilenameTextResourceKey.Trim();

        if (SubformComponentId.IsNullOrWhitespace(ref errorMessages, $"{nameof(SubformComponentId)} is missing."))
            ThrowApplicationConfigException(errorMessages);

        if (SubformDataTypeId.IsNullOrWhitespace(ref errorMessages, $"{nameof(SubformDataTypeId)} is missing."))
            ThrowApplicationConfigException(errorMessages);

        if (DegreeOfParallelism < 1)
        {
            errorMessages ??= [];
            errorMessages.Add($"{nameof(DegreeOfParallelism)} must be at least 1.");
            ThrowApplicationConfigException(errorMessages);
        }

        return new ValidAltinnSubformPdfConfiguration(
            normalizedFilename,
            SubformComponentId,
            SubformDataTypeId,
            DegreeOfParallelism
        );
    }

    [DoesNotReturn]
    private static void ThrowApplicationConfigException(List<string> errorMessages)
    {
        throw new ApplicationConfigException(
            "Subform pdf service task configuration is not valid: " + string.Join(",\n", errorMessages)
        );
    }
}

internal readonly record struct ValidAltinnSubformPdfConfiguration(
    string? FilenameTextResourceKey,
    string SubformComponentId,
    string SubformDataTypeId,
    int DegreeOfParallelism
);

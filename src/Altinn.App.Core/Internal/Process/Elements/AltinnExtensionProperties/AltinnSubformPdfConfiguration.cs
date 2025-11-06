using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using Altinn.App.Core.Internal.App;

namespace Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;

/// <summary>
/// Configuration properties for payments in a process task
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
    [XmlElement("subformDatatTypeId", Namespace = "http://altinn.no/process")]
    public string? SubformDataTypeId { get; set; }

    /// <summary>
    /// Whether to generate PDFs for multiple subform data elements in parallel or sequentially.
    /// Defaults to false (sequential) to avoid overwhelming the PDF microservice.
    /// Set to true only if your PDF microservice can handle concurrent requests.
    /// </summary>
    [XmlElement("parallelExecution", Namespace = "http://altinn.no/process")]
    public bool ParallelExecution { get; set; }

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

        return new ValidAltinnSubformPdfConfiguration(
            normalizedFilename,
            SubformComponentId,
            SubformDataTypeId,
            ParallelExecution
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
    bool ParallelExecution
);

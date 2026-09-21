using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models.Calculation;

/// <summary>
/// Represents the schema for the calculation configuration
/// </summary>
public class CalculationSchema
{
    /// <summary>
    /// The JSON schema that describes the calculation configuration file.
    /// </summary>
    public const string Schema =
        "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/calculation/calculation.schema.v1.json";

    /// <summary>
    /// Gets or sets the list of calculation items in the calculation configuration.
    /// </summary>
    [JsonPropertyName("calculations")]
    public required List<CalculationItem> Calculations { get; init; }
}

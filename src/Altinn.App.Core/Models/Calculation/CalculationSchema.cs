using System.Text.Json.Serialization;

namespace Altinn.App.Core.Models.Calculation;

/// <summary>
/// Represents the schema for the calculation configuration
/// </summary>
public class CalculationSchema
{
    /// <summary>
    /// Gets the schema for the calculation configuration.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema =>
        "https://altinncdn.no/toolkits/altinn-app-frontend/4/schemas/json/calculation/calculation.schema.v1.json";

    /// <summary>
    /// Gets or sets the list of calculation items in the calculation configuration.
    /// </summary>
    public required List<CalculationItem> Calculations { get; init; }
}

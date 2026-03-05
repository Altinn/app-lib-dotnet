using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models;

/// <summary>
/// Resolved data field calculation
/// </summary>
public class DataFieldCalculation
{
    /// <summary>
    /// Condition to evaluate
    /// </summary>
    public Expression Condition { get; set; }
}

/// <summary>
/// Raw value calculation expression from the calculaiton configuration file
/// </summary>
public class RawDataFieldValueCalculation
{
    /// <summary>
    /// Condition to evaluate
    /// </summary>
    public Expression? Condition { get; set; }

    /// <summary>
    /// Reference to expression definitions
    /// </summary>
    public string? Ref { get; set; }
}

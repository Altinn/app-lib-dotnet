using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models;

/// <summary>
/// Resolved data field calculation
/// </summary>
internal sealed class DataModelFieldCalculation
{
    /// <summary>
    /// Condition to evaluate
    /// </summary>
    public required Expression Condition { get; set; }
}

/// <summary>
/// Raw value calculation expression from the calculation configuration file
/// </summary>
internal sealed class RawDataModelFieldCalculation
{
    /// <summary>
    /// Condition to evaluate
    /// </summary>
    public Expression? Condition { get; set; }
}

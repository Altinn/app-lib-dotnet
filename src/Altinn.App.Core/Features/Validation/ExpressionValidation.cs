using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Validation;


namespace Altinn.App.Core.Features.Validation
{

    /// <summary>
    /// Resolved expression validation
    /// </summary>
    public class ExpressionValidation
    {
        /// <inheritdoc/>
        public string Message { get; set; }
        /// <inheritdoc/>
        public Expression Condition { get; set; }
        /// <inheritdoc/>
        public ValidationIssueSeverity Severity { get; set; }
    }

    /// <summary>
    /// Raw expression validation or definition from the validation configuration file
    /// </summary>
    public class RawExpressionValidation
    {
        /// <inheritdoc/>
        public string? Message { get; set; }
        /// <inheritdoc/>
        public Expression? Condition { get; set; }
        /// <inheritdoc/>
        public string? Severity { get; set; }
        /// <inheritdoc/>
        public string? Ref { get; set; }
    }
}

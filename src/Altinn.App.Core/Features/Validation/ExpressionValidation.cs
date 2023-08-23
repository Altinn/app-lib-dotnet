using System.Text.Json.Serialization;
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
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        /// <inheritdoc/>
        [JsonPropertyName("condition")]
        public Expression? Condition { get; set; }
        /// <inheritdoc/>
        [JsonPropertyName("severity")]
        public string? Severity { get; set; }
        /// <inheritdoc/>
        [JsonPropertyName("ref")]
        public string? Ref { get; set; }
    }
}

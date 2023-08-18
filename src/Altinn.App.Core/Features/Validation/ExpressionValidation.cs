using Altinn.App.Core.Models.Expressions;
using Altinn.App.Core.Models.Validation;


namespace Altinn.App.Core.Features.Validation
{

    public class ExpressionValidation
    {
        public string Message { get; set; }
        public Expression Condition { get; set; }
        public ValidationIssueSeverity Severity { get; set; }
    }

    public class RawExpressionValidation
    {
        public string Message { get; set; }
        public Expression Condition { get; set; }
        public ValidationIssueSeverity? Severity { get; set; }
    }
}

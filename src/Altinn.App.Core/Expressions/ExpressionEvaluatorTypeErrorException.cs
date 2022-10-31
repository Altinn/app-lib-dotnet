namespace Altinn.App.Core.Expressions;

/// <summary>
/// Custom exception for <see cref="ExpressionEvaluator" /> to thow when expressions contains type errors.
/// </summary>
public class ExpressionEvaluatorTypeErrorException : Exception
{
    /// <inheritdoc />
    public ExpressionEvaluatorTypeErrorException(string msg) : base(msg) {}
}
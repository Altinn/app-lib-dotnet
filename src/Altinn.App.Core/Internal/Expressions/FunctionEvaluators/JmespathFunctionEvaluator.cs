using System.Text.Json;
using DevLab.JmesPath;

namespace Altinn.App.Core.Internal.Expressions.FunctionEvaluators;

internal sealed class JmespathFunctionEvaluator
{
    private readonly ExpressionValue[] _args;

    public JmespathFunctionEvaluator(ExpressionValue[] args) => _args = args;

    public ExpressionValue Evaluate()
    {
        if (_args.Length != 2)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected 2 argument(s), got {_args.Length}");
        }
        if (_args[1].ValueKind != JsonValueKind.String)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Expected argument to be string, got {_args[1]}");
        }
        return EvaluateWithValidArguments(_args[0], _args[1].String);
    }

    private static ExpressionValue EvaluateWithValidArguments(ExpressionValue data, string query)
    {
        JmesPath jmesPath = new();
        try
        {
            string resultAsString = jmesPath.Transform(data.ToString(), query);
            return ExpressionValue.FromJsonString(resultAsString);
        }
        catch (Exception exception)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Jmespath error: \"{exception.Message}\"");
        }
    }
}

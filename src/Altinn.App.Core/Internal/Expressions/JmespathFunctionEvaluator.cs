using System.Text.Json;
using DevLab.JmesPath;

namespace Altinn.App.Core.Internal.Expressions;

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
        return Implementation(_args[0], _args[1].String);
    }

    private static ExpressionValue Implementation(ExpressionValue data, string query)
    {
        string resultAsString = VerifyAndRunQuery(data, query);
        JsonElement result = JsonSerializer.Deserialize<JsonElement>(resultAsString);
        return ExpressionValue.FromJsonElement(result);
    }

    private static string VerifyAndRunQuery(ExpressionValue data, string query)
    {
        JmesPath jmesPath = new();
        try
        {
            return jmesPath.Transform(data.ToString(), query);
        }
        catch (Exception exception)
        {
            throw new ExpressionEvaluatorTypeErrorException($"Jmespath error: \"{exception.Message}\"");
        }
    }
}

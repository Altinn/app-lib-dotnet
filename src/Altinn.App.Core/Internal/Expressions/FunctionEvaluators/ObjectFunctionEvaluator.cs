using System.Text.Json;
using System.Text.Json.Nodes;

namespace Altinn.App.Core.Internal.Expressions.FunctionEvaluators;

internal sealed class ObjectFunctionEvaluator
{
    private readonly ExpressionValue[] _args;

    public ObjectFunctionEvaluator(ExpressionValue[] args) => _args = args;

    public JsonObject Evaluate()
    {
        AssertEvenNumberOfArguments();
        string[] keys = ExtractKeys();
        AssertKeysAreUnique(keys);
        JsonNode?[] values = ExtractValues();
        Dictionary<string, JsonNode?> keyValuePairs = DictionaryFromKeysAndValues(keys, values);
        return new JsonObject(keyValuePairs);
    }

    private void AssertEvenNumberOfArguments()
    {
        if (_args.Length % 2 == 1)
        {
            throw new ExpressionEvaluatorTypeErrorException(
                "The object function must have an even number of arguments."
            );
        }
    }

    private string[] ExtractKeys()
    {
        try
        {
            return _args.Where((_, index) => index % 2 == 0).Select(v => v.String).ToArray();
        }
        catch (InvalidCastException)
        {
            throw new ExpressionEvaluatorTypeErrorException("Object keys must be strings.");
        }
    }

    private static void AssertKeysAreUnique(string[] keys)
    {
        if (keys.Length != keys.Distinct().Count())
        {
            throw new ExpressionEvaluatorTypeErrorException("Object keys must be unique.");
        }
    }

    private JsonNode?[] ExtractValues() =>
        _args.Where((_, index) => index % 2 == 1).Select(v => JsonSerializer.SerializeToNode(v)).ToArray();

    private static Dictionary<string, JsonNode?> DictionaryFromKeysAndValues(string[] keys, JsonNode?[] values) =>
        keys.Zip(values, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
}

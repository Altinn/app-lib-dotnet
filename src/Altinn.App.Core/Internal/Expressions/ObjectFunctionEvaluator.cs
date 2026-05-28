namespace Altinn.App.Core.Internal.Expressions;

internal class ObjectFunctionEvaluator
{
    private readonly ExpressionValue[] _args;

    public ObjectFunctionEvaluator(ExpressionValue[] args) => _args = args;

    public Dictionary<string, ExpressionValue> Evaluate()
    {
        AssertEvenNumberOfArguments();
        string[] keys = ExtractKeys();
        AssertKeysAreUnique(keys);
        ExpressionValue[] values = ExtractValues();
        return keys.Zip(values, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
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

    private ExpressionValue[] ExtractValues() => _args.Where((_, index) => index % 2 == 1).ToArray();
}

using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Altinn.App.TestUtils.JsonCompare;

public static class FluentAssertionsExtentions
{
    public static AndConstraint<StringAssertions> BeJsonEquivalentTo(this StringAssertions actual, string expected, JsonComparatorOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(actual, "Cannot assert string containment against <null>.");

        string? result;
        if (options?.LooseObjectOrderComparison != true)
        {
            result = JsonTokenStreamCompare.IsJsonTokenEquivalent(actual.Subject, expected, options);
        }
        else
        {
            throw new NotSupportedException(); // Not implemented yet
        }

        if (result is not null)
        {
            Execute.Assertion
                .FailWith(() => new FailReason("Expected {context:string} to match expected json, but found diff\n{0}", result));
        }

        return new AndConstraint<StringAssertions>(actual);
    }
}
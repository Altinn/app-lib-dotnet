using System.Diagnostics.CodeAnalysis;
using Altinn.App.Core.Features.Correspondence.Exceptions;

namespace Altinn.App.Core.Features.Correspondence.Builder;

internal static class BuilderUtils
{
    /// <summary>
    /// <p>Because of the interface-chaining in this builder, some properties are guaranteed to be non-null.
    /// But the compiler doesn't trust that, so we add this check where needed.</p>
    /// <p>Additionally, this method checks for empty strings and empty data allocations.</p>
    /// </summary>
    /// <param name="value">The value to assert</param>
    /// <param name="errorMessage">The error message to throw, if the value was null</param>
    /// <exception cref="CorrespondenceArgumentException"></exception>
    internal static void NotNullOrEmpty([NotNull] object? value, string? errorMessage = null)
    {
        if (
            value is null
            || value is string str && string.IsNullOrWhiteSpace(str)
            || value is ReadOnlyMemory<byte> { IsEmpty: true }
            || value is DateTimeOffset dt && dt == DateTimeOffset.MinValue
        )
        {
            throw new CorrespondenceArgumentException(errorMessage);
        }
    }
}

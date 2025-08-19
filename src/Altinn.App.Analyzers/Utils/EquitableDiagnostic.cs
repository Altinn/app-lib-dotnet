// Copied from PolyType with some inlined helper methods

namespace Altinn.App.Analyzers.Utils;

/// <summary>
/// Descriptor for diagnostic instances using structural equality comparison.
/// Provides a work-around for https://github.com/dotnet/roslyn/issues/68291.
/// </summary>
public readonly struct EquatableDiagnostic(DiagnosticDescriptor descriptor, Location? location, object?[] messageArgs)
    : IEquatable<EquatableDiagnostic>
{
    /// <summary>
    /// The <see cref="DiagnosticDescriptor"/> for the diagnostic.
    /// </summary>
    public DiagnosticDescriptor Descriptor { get; } = descriptor;

    /// <summary>
    /// The message arguments for the diagnostic.
    /// </summary>
    public object?[] MessageArgs { get; } = messageArgs;

    /// <summary>
    /// The location of the diagnostic.
    /// </summary>
    public Location? Location { get; } = GetLocationTrimmed(location);

    // Inline utility method from PolyType Helpers.RoslynHelpers
    private static Location? GetLocationTrimmed(Location? location)
    {
        if (location is null)
        {
            return null;
        }
        var lineSpan = location.GetLineSpan();
        var mappedSpan = location.GetMappedLineSpan();

        return Location.Create(lineSpan.Path, location.SourceSpan, lineSpan.Span, mappedSpan.Path, mappedSpan.Span);
    }

    /// <summary>
    /// Creates a new <see cref="Diagnostic"/> instance from the current instance.
    /// </summary>
    public Diagnostic CreateDiagnostic() => Diagnostic.Create(Descriptor, Location, MessageArgs);

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is EquatableDiagnostic info && Equals(info);

    /// <inheritdoc/>
    public readonly bool Equals(EquatableDiagnostic other)
    {
        return Descriptor.Equals(other.Descriptor)
            && MessageArgs.SequenceEqual(other.MessageArgs)
            && Location == other.Location;
    }

    /// <inheritdoc/>
    public readonly override int GetHashCode()
    {
        int hashCode = Descriptor.GetHashCode();
        foreach (object? messageArg in MessageArgs)
        {
            hashCode = CombineHashCodes(hashCode, messageArg?.GetHashCode() ?? 0);
        }

        hashCode = CombineHashCodes(hashCode, Location?.GetHashCode() ?? 0);
        return hashCode;
    }

    // inline utility method from PolyType Helpers.CommonHelpers
    private static int CombineHashCodes(int h1, int h2)
    {
        // RyuJIT optimizes this to use the ROL instruction
        // Related GitHub pull request: https://github.com/dotnet/coreclr/pull/1830
        uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
        return ((int)rol5 + h1) ^ h2;
    }

    public static bool operator ==(EquatableDiagnostic left, EquatableDiagnostic right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EquatableDiagnostic left, EquatableDiagnostic right)
    {
        return !left.Equals(right);
    }
}

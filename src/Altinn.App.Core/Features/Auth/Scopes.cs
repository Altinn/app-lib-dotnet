using System.Buffers;

namespace Altinn.App.Core.Features.Auth;

internal readonly struct Scopes : IEquatable<Scopes>
{
    private readonly string? _scope;

    public Scopes(string? scope) => _scope = scope;

    public bool Equals(Scopes other) => _scope == other._scope;

    public override bool Equals(object? obj) => obj is Scopes other ? Equals(other) : false;

    public override int GetHashCode() => _scope?.GetHashCode() ?? 0;

    public override string ToString() => _scope ?? "";

    private static readonly SearchValues<char> _whitespace = SearchValues.Create(" \t\n");
    private static readonly SearchValues<char> _alphaNumeric = SearchValues.Create(
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZÆØÅabcdefghijklmnopqrstuvwxyzæøå"
    );

    public bool HasScope(string scopeToCheck)
    {
        if (string.IsNullOrWhiteSpace(_scope))
            return false;

        ReadOnlySpan<char> scopes = _scope.AsSpan();

        while (true)
        {
            var spaceIndex = scopes.IndexOfAny(_whitespace);
            ReadOnlySpan<char> currentScope;
            if (spaceIndex == -1)
            {
                currentScope = scopes;
                scopes = ReadOnlySpan<char>.Empty;
            }
            else
            {
                currentScope = scopes.Slice(0, spaceIndex);
                var nextNonWhitespace = scopes.Slice(spaceIndex).IndexOfAny(_alphaNumeric);
                if (nextNonWhitespace != -1)
                    scopes = scopes.Slice(spaceIndex + nextNonWhitespace);
                else
                    scopes = ReadOnlySpan<char>.Empty;
            }

            if (currentScope.Equals(scopeToCheck, StringComparison.OrdinalIgnoreCase))
                return true;

            if (scopes.IsEmpty)
                break;
        }

        return false;
    }
}

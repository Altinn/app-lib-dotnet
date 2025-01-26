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

    public WordEnumerator GetEnumerator() => new WordEnumerator(_scope.AsSpan());

    public ref struct WordEnumerator
    {
        private ReadOnlySpan<char> _scopes;
        private ReadOnlySpan<char> _currentScope;

        public WordEnumerator(ReadOnlySpan<char> scopes)
        {
            _scopes = scopes;
            _currentScope = ReadOnlySpan<char>.Empty;
        }

        public readonly ReadOnlySpan<char> Current => _currentScope;

        public bool MoveNext()
        {
            if (_scopes.IsEmpty)
                return false;

            var spaceIndex = _scopes.IndexOfAny(_whitespace);
            if (spaceIndex == -1)
            {
                _currentScope = _scopes;
                _scopes = ReadOnlySpan<char>.Empty;
            }
            else
            {
                _currentScope = _scopes.Slice(0, spaceIndex);
                var nextNonWhitespace = _scopes.Slice(spaceIndex).IndexOfAny(_alphaNumeric);
                if (nextNonWhitespace != -1)
                    _scopes = _scopes.Slice(spaceIndex + nextNonWhitespace);
                else
                    _scopes = ReadOnlySpan<char>.Empty;
            }

            return true;
        }
    }

    public bool HasScope(string scopeToFind)
    {
        if (string.IsNullOrWhiteSpace(_scope))
            return false;

        foreach (var scope in this)
        {
            if (scope.Equals(scopeToFind, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool HasScopePrefix(string scopePrefix)
    {
        if (string.IsNullOrWhiteSpace(_scope))
            return false;

        foreach (var scope in this)
        {
            if (scope.StartsWith(scopePrefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

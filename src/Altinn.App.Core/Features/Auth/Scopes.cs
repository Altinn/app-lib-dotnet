using System.Buffers;

namespace Altinn.App.Core.Features.Auth;

/// <summary>
/// Collection of scopes from a JWT token.
/// </summary>
public readonly struct Scopes : IEquatable<Scopes>
{
    /// <summary>
    /// Empty scopes.
    /// </summary>
    public static readonly Scopes None = new Scopes(null);

    private readonly string? _scope;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scopes"/> struct.
    /// </summary>
    /// <param name="scope"></param>
    public Scopes(string? scope) => _scope = scope;

    /// <summary>
    /// Compares two <see cref="Scopes"/> objects for equality.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Scopes other) => _scope == other._scope;

    /// <summary>
    /// Compares two <see cref="Scopes"/> objects for equality.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj) => obj is Scopes other ? Equals(other) : false;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _scope?.GetHashCode() ?? 0;

    /// <summary>
    /// Compares two <see cref="Scopes"/> objects for equality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Scopes left, Scopes right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two <see cref="Scopes"/> objects for equality.
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Scopes left, Scopes right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => _scope ?? "";

    private static readonly SearchValues<char> _whitespace = SearchValues.Create(" \t\n");
    private static readonly SearchValues<char> _alphaNumeric = SearchValues.Create(
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZÆØÅabcdefghijklmnopqrstuvwxyzæøå"
    );

    /// <summary>
    /// Returns an enumerator that iterates through the scopes.
    /// </summary>
    /// <returns></returns>
    public ScopeEnumerator GetEnumerator() => new ScopeEnumerator(_scope.AsSpan());

    /// <summary>
    /// Enumerator for iterating through the scopes.
    /// </summary>
    public ref struct ScopeEnumerator
    {
        private ReadOnlySpan<char> _scopes;
        private ReadOnlySpan<char> _currentScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeEnumerator"/> struct.
        /// </summary>
        /// <param name="scopes"></param>
        public ScopeEnumerator(ReadOnlySpan<char> scopes)
        {
            _scopes = scopes;
            _currentScope = ReadOnlySpan<char>.Empty;
        }

        /// <summary>
        /// Gets the current scope of the enumerator.
        /// </summary>
        public readonly ReadOnlySpan<char> Current => _currentScope;

        /// <summary>
        /// Moves to the next scope in the enumerator.
        /// </summary>
        /// <returns></returns>
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

    /// <summary>
    /// Checks if the scopes contains a specific scope.
    /// </summary>
    /// <param name="scopeToFind">the scope to search for</param>
    /// <returns></returns>
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

    /// <summary>
    /// Checks if any of the scopes contains a specific scope prefix.
    /// </summary>
    /// <param name="scopePrefix">the prefix to search for</param>
    /// <returns></returns>
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

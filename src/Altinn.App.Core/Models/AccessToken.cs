using System.Text.RegularExpressions;

namespace Altinn.App.Core.Models;

/// <summary>
/// Represents an OAuth 2.0 access token in JWT format
/// </summary>
public readonly partial struct AccessToken : IEquatable<AccessToken>
{
    /// <summary>
    /// The access token value (JWT format)
    /// </summary>
    public string Value { get; }

    private AccessToken(string accessToken)
    {
        Value = accessToken;
    }

    /// <summary>
    /// Parses an access token
    /// </summary>
    /// <param name="value">The value to parse</param>
    /// <exception cref="FormatException">The access token is not valid</exception>
    public static AccessToken Parse(string value)
    {
        return TryParse(value, out var accessToken)
            ? accessToken
            : throw new FormatException($"Invalid access token format: {value}");
    }

    /// <summary>
    /// Attempt to parse an access token
    /// </summary>
    /// <param name="value">The value to parse</param>
    /// <param name="accessToken">The resulting <see cref="AccessToken"/> instance</param>
    /// <returns>`true` on successful parse, `false` otherwise</returns>
    public static bool TryParse(string value, out AccessToken accessToken)
    {
        accessToken = default;

        if (JwtRegex().IsMatch(value) is false)
        {
            return false;
        }

        accessToken = new AccessToken(value);
        return true;
    }

    // Matches a string with pattern eyXXX.eyXXX.XXX, allowing underscores and hyphens
    [GeneratedRegex(@"^((?:ey[\w-]+\.){2})([\w-]+)$")]
    private static partial Regex JwtRegex();

    private static string MaskJwtSignature(string accessToken)
    {
        var accessTokenMatch = JwtRegex().Match(accessToken);
        return accessTokenMatch.Success ? $"{accessTokenMatch.Groups[1]}<masked>" : "<masked>";
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    public bool Equals(AccessToken other) => Value == other.Value;

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    public override bool Equals(object? obj) => obj is AccessToken other && Equals(other);

    /// <summary>
    /// Returns the hash code for the access token value
    /// </summary>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// Returns a string representation of the access token with a masked signature component
    /// </summary>
    public override string ToString() => MaskJwtSignature(Value);

    /// <summary>
    /// Returns a string representation of the access token with an intact signature component
    /// </summary>
    public string ToStringUnmasked() => Value;

    /// <summary>
    /// Determines whether two specified instances of <see cref="AccessToken"/> are equal
    /// </summary>
    public static bool operator ==(AccessToken left, AccessToken right) => left.Equals(right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="AccessToken"/> are not equal
    /// </summary>
    public static bool operator !=(AccessToken left, AccessToken right) => !left.Equals(right);

    /// <summary>
    /// Implicit conversion from <see cref="AccessToken"/> to string
    /// </summary>
    /// <param name="accessToken">The access token instance</param>
    public static implicit operator string(AccessToken accessToken)
    {
        return accessToken.Value;
    }
}

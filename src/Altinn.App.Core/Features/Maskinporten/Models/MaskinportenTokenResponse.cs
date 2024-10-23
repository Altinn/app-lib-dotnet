using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Altinn.App.Core.Features.Maskinporten.Models;

/// <summary>
/// Contains masking logic for JWTs
/// </summary>
internal static partial class JwtMasking
{
    internal static Regex JwtRegex => JwtRegexFactory();

    internal static string MaskSignature(string accessToken)
    {
        var accessTokenMatch = JwtRegex.Match(accessToken);
        return accessTokenMatch.Success ? $"{accessTokenMatch.Groups[1]}.{accessTokenMatch.Groups[2]}.xxx" : "<masked>";
    }

    [GeneratedRegex(@"^(.+)\.(.+)\.(.+)$", RegexOptions.Multiline)]
    private static partial Regex JwtRegexFactory();
}

public interface ITokenResponse
{
    string AccessToken { get; init; }
}

/// <summary>
/// The response received from Maskinporten after a successful grant request.
/// </summary>
[ImmutableObject(true)]
public sealed record MaskinportenTokenResponse : ITokenResponse
{
    /// <summary>
    /// The JWT access token to be used in the Authorization header for downstream requests.
    /// </summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    /// <summary>
    /// The type of JWT received. In this context, the value is always `Bearer`.
    /// </summary>
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }

    /// <summary>
    /// The number of seconds until token expiry. Typically set to 120 = 2 minutes.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }

    /// <summary>
    /// The scope(s) associated with the authorization token (<see cref="AccessToken"/>).
    /// </summary>
    [JsonPropertyName("scope")]
    public required string Scope { get; init; }

    /// <summary>
    /// Convenience conversion of <see cref="ExpiresIn"/> to an actual instant in time.
    /// </summary>
    public DateTime ExpiresAt => CreatedAt.AddSeconds(ExpiresIn);

    /// <summary>
    /// Internal tracker used by <see cref="ExpiresAt"/> to calculate an expiry <see cref="DateTime"/>.
    /// </summary>
    internal DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Is the token expired?
    /// </summary>
    public bool IsExpired(TimeProvider? timeProvider = null) =>
        ExpiresAt < (timeProvider?.GetUtcNow().UtcDateTime ?? DateTime.UtcNow);

    /// <summary>
    /// Stringifies the content of this instance, while masking the JWT signature part of <see cref="AccessToken"/>
    /// </summary>
    public override string ToString()
    {
        var maskedToken = JwtMasking.MaskSignature(AccessToken);
        return $"{nameof(AccessToken)}: {maskedToken}, {nameof(TokenType)}: {TokenType}, {nameof(Scope)}: {Scope}, {nameof(ExpiresIn)}: {ExpiresIn}, {nameof(ExpiresAt)}: {ExpiresAt}";
    }

    // [GeneratedRegex(@"^(.+)\.(.+)\.(.+)$", RegexOptions.Multiline)]
    // private static partial Regex JwtRegexFactory();
}

/// <summary>
/// The response received from Altinn Authentication after exchanging a Maskinporten token for an Altinn token.
/// </summary>
[ImmutableObject(true)]
public sealed record MaskinportenAltinnExchangedTokenResponse : ITokenResponse
{
    /// <summary>
    /// The JWT access token to be used in the Authorization header for downstream requests.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The instant in time when the token expires.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// The scope(s) associated with the authorization token (<see cref="AccessToken"/>).
    /// </summary>
    public required string? Scope { get; init; }

    /// <summary>
    /// Is the token expired?
    /// </summary>
    public bool IsExpired(TimeProvider? timeProvider = null) =>
        ExpiresAt < (timeProvider?.GetUtcNow().UtcDateTime ?? DateTime.UtcNow);

    /// <summary>
    /// Stringifies the content of this instance, while masking the JWT signature part of <see cref="AccessToken"/>
    /// </summary>
    public override string ToString()
    {
        var maskedToken = JwtMasking.MaskSignature(AccessToken);
        return $"{nameof(AccessToken)}: {maskedToken}, {nameof(Scope)}: {Scope}, {nameof(ExpiresAt)}: {ExpiresAt}";
    }
}

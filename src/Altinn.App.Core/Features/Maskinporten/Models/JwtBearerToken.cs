using System.ComponentModel;
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

/// <summary>
/// An object that contains a JWT <see cref="AccessToken"/> member
/// </summary>
public interface IAccessToken
{
    /// <summary>
    /// The JWT access token to be used in the Authorization header for downstream requests
    /// </summary>
    string AccessToken { get; init; }
}

/// <summary>
/// JWT Bearer token wrapper which includes the <see cref="AccessToken"/> and relevant metadata
/// </summary>
[ImmutableObject(true)] // `ImmutableObject` prevents serialization with HybridCache
public sealed record JwtBearerToken : IAccessToken
{
    /// <summary>
    /// The JWT access token to be used in the Authorization header for http requests
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The instant in time when the token expires
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// The scope(s) associated with the <see cref="AccessToken"/>
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
        string maskedToken = JwtMasking.MaskSignature(AccessToken);
        return $"{nameof(AccessToken)}: {maskedToken}, {nameof(Scope)}: {Scope}, {nameof(ExpiresAt)}: {ExpiresAt}";
    }
}

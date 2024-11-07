using System.ComponentModel;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Maskinporten.Models;

/// <summary>
/// An object that contains a JWT <see cref="AccessToken"/> member
/// </summary>
public interface IAccessToken
{
    /// <summary>
    /// The JWT access token to be used in the Authorization header for downstream requests
    /// </summary>
    AccessToken AccessToken { get; init; }
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
    public required AccessToken AccessToken { get; init; }

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
        return $"{nameof(AccessToken)}: {AccessToken}, {nameof(Scope)}: {Scope}, {nameof(ExpiresAt)}: {ExpiresAt}";
    }

    /// <summary>
    /// Implicit conversion from <see cref="JwtBearerToken"/> to <see cref="AccessToken"/>
    /// </summary>
    /// <param name="jwtBearerToken">The JWT bearer token instance</param>
    public static implicit operator AccessToken(JwtBearerToken jwtBearerToken)
    {
        return jwtBearerToken.AccessToken;
    }
}

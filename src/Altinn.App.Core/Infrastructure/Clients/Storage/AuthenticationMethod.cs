using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Infrastructure.Clients.Storage;

/// <summary>
/// Represents the method of authentication to be used for making requests to external services.
/// </summary>
public abstract record AuthenticationMethod
{
    private static readonly string[] _serviceOwnerScopes =
    [
        "altinn:serviceowner/instances.read",
        "altinn:serviceowner/instances.write",
    ];

    /// <inheritdoc cref="AuthenticationMethod.UserToken"/>
    public static UserToken CurrentUser() => new();

    /// <summary>
    /// Indicates that an operation should be authenticated using service owner `read` and `write` scopes.
    /// </summary>
    public static AltinnToken ServiceOwner() => new(_serviceOwnerScopes);

    /// <summary>
    /// Indicates that an operation should be authenticated using service owner `read` and `write` scopes, with additional scopes if provided.
    /// </summary>
    public static AltinnToken ServiceOwner(params string[] additionalScopes) =>
        new([.. _serviceOwnerScopes, .. additionalScopes]);

    /// <inheritdoc cref="AuthenticationMethod.MaskinportenToken"/>
    public static MaskinportenToken Maskinporten(string scope, params string[] additionalScopes) =>
        new([scope, .. additionalScopes]);

    /// <inheritdoc cref="AuthenticationMethod.CustomToken"/>
    public static CustomToken Custom(Func<Task<JwtToken>> tokenProvider) => new(tokenProvider);

    /// <summary>
    /// Indicates that an operation should be authenticated using the current user's token
    /// </summary>
    public sealed record UserToken : AuthenticationMethod
    {
        internal UserToken() { }
    }

    /// <summary>
    /// Indicates that an operation should be authenticated using a token with the specified scopes,
    /// provided by <see cref="MaskinportenClient.GetAltinnExchangedToken">MaskinportenClient.GetAltinnExchangedToken</see>.
    /// </summary>
    public sealed record AltinnToken : AuthenticationMethod
    {
        /// <summary>
        /// The scopes associated with this request.
        /// </summary>
        public string[] Scopes { get; }

        /// <summary>
        /// <inheritdoc cref="AuthenticationMethod.AltinnToken"/>
        /// </summary>
        /// <param name="scopes">The scopes to claim authorization for</param>
        internal AltinnToken(params string[] scopes)
        {
            Scopes = scopes;
        }
    }

    /// <summary>
    /// Indicates that an operation should be authenticated using a token with the specified scopes,
    /// provided by <see cref="MaskinportenClient.GetAccessToken">MaskinportenClient.GetAccessToken</see>.
    /// </summary>
    public sealed record MaskinportenToken : AuthenticationMethod
    {
        /// <summary>
        /// The scopes associated with this request.
        /// </summary>
        public string[] Scopes { get; }

        /// <summary>
        /// <inheritdoc cref="AuthenticationMethod.MaskinportenToken"/>
        /// </summary>
        /// <param name="scopes">The scopes to claim authorization for</param>
        internal MaskinportenToken(params string[] scopes)
        {
            Scopes = scopes;
        }
    }

    /// <summary>
    /// Indicates that an operation should be authenticated using a custom token provider.
    /// </summary>
    public sealed record CustomToken : AuthenticationMethod
    {
        /// <summary>
        /// The JWT token provider for this request.
        /// </summary>
        public Func<Task<JwtToken>> TokenProvider { get; }

        /// <summary>
        /// <inheritdoc cref="AuthenticationMethod.CustomToken"/>
        /// </summary>
        /// <param name="tokenProvider">The delegate providing the token when invoked</param>
        internal CustomToken(Func<Task<JwtToken>> tokenProvider)
        {
            TokenProvider = tokenProvider;
        }
    }
}

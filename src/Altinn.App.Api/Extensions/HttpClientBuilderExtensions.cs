using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Delegates;

namespace Altinn.App.Api.Extensions;

/// <summary>
/// Altinn specific extensions for <see cref="IHttpClientBuilder"/>
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// <para>
    /// Authorises all requests with Maskinporten using the provided scopes,
    /// and injects the resulting token in the Authorization header using the Bearer scheme.
    /// </para>
    /// <para>
    /// If your target API does <em>not</em> use this authorisation scheme, you should consider implementing
    /// <see cref="MaskinportenClient.GetAccessToken"/> directly and handling the specifics manually.
    /// </para>
    /// </summary>
    /// <param name="builder">The Http client builder</param>
    /// <param name="scope">The scope to claim authorization for with Maskinporten</param>
    /// <param name="additionalScopes">Additional scopes as required</param>
    public static IHttpClientBuilder UseMaskinportenAuthorisation(
        this IHttpClientBuilder builder,
        string scope,
        params string[] additionalScopes
    )
    {
        return AddHttpMessageHandler(builder, scope, additionalScopes, TokenAuthority.Maskinporten);
    }

    /// <summary>
    /// <para>
    /// Authorises all requests with Maskinporten using the provided scopes.
    /// The resulting token is then exchanged for an Altinn issued token and injected in
    /// the Authorization header using the Bearer scheme.
    /// </para>
    /// <para>
    /// If your target API does <em>not</em> use this authorisation scheme, you should consider implementing
    /// <see cref="MaskinportenClient.GetAltinnExchangedToken(IEnumerable{string}, CancellationToken)"/> directly and handling the specifics manually.
    /// </para>
    /// </summary>
    /// <param name="builder">The Http client builder</param>
    /// <param name="scope">The scope to claim authorization for with Maskinporten</param>
    /// <param name="additionalScopes">Additional scopes as required</param>
    public static IHttpClientBuilder UseMaskinportenAltinnAuthorisation(
        this IHttpClientBuilder builder,
        string scope,
        params string[] additionalScopes
    )
    {
        return AddHttpMessageHandler(builder, scope, additionalScopes, TokenAuthority.AltinnTokenExchange);
    }

    private static IHttpClientBuilder AddHttpMessageHandler(
        IHttpClientBuilder builder,
        string scope,
        string[] additionalScopes,
        TokenAuthority authority
    )
    {
        var scopes = new[] { scope }.Concat(additionalScopes);
        var factory = ActivatorUtilities.CreateFactory<MaskinportenDelegatingHandler>(
            [typeof(TokenAuthority), typeof(IEnumerable<string>),]
        );
        return builder.AddHttpMessageHandler(provider => factory(provider, [authority, scopes]));
    }

    /// <inheritdoc cref="UseMaskinportenAuthorisation"/>
    [Obsolete("UseMaskinportenAuthorization is deprecated, use UseMaskinportenAuthorisation instead.")]
    public static IHttpClientBuilder UseMaskinportenAuthorization(
        this IHttpClientBuilder builder,
        string scope,
        params string[] additionalScopes
    )
    {
        return UseMaskinportenAuthorisation(builder, scope, additionalScopes);
    }
}

using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents the payload for sending a correspondence
/// </summary>
public sealed record SendCorrespondencePayload
{
    /// <summary>
    /// The correspondence request to send
    /// </summary>
    public required CorrespondenceRequest CorrespondenceRequest { get; init; }

    /// <summary>
    /// Access token factory delegate (e.g. <see cref="MaskinportenClient.GetAltinnExchangedToken"/>)
    /// </summary>
    public required Func<Task<AccessToken>> AccessTokenFactory { get; init; }

    /// <summary>
    /// The payload contains a <see cref="Models.CorrespondenceRequest"/> instance
    /// </summary>
    /// <param name="correspondenceRequest">The correspondence request to send</param>
    /// <param name="accessTokenFactory">Access token factory delegate (e.g. <see cref="MaskinportenClient.GetAltinnExchangedToken"/>)</param>
    public static SendCorrespondencePayload WithRequest(
        CorrespondenceRequest correspondenceRequest,
        Func<Task<AccessToken>> accessTokenFactory
    )
    {
        return new SendCorrespondencePayload
        {
            CorrespondenceRequest = correspondenceRequest,
            AccessTokenFactory = accessTokenFactory
        };
    }

    // TODO: `WithRequest` overloads for built-in authz schemes here

    /// <summary>
    /// The payload contains an <see cref="ICorrespondenceRequestBuilder"/> builder instance
    /// </summary>
    /// <param name="correspondenceRequestBuilder">The correspondence request builder to send</param>
    /// <param name="accessTokenFactory">Access token factory delegate (e.g. <see cref="MaskinportenClient.GetAltinnExchangedToken"/>)</param>
    public static SendCorrespondencePayload WithBuilder(
        ICorrespondenceRequestBuilder correspondenceRequestBuilder,
        Func<Task<AccessToken>> accessTokenFactory
    )
    {
        return new SendCorrespondencePayload
        {
            CorrespondenceRequest = correspondenceRequestBuilder.Build(),
            AccessTokenFactory = accessTokenFactory
        };
    }

    // TODO: `WithBuilder` overloads for built-in authz schemes here
}

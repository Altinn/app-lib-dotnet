using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Represents the payload for sending a correspondence
/// </summary>
public sealed record SendCorrespondencePayload
{
    private readonly CorrespondenceRequest _correspondenceRequest;
    private readonly Func<Task<AccessToken>> _accessTokenFactory;

    /// <summary>
    /// Access token factory delegate (e.g. <see cref="MaskinportenClient.GetAltinnExchangedToken"/>)
    /// </summary>
    public Func<Task<AccessToken>> AccessTokenFactory => _accessTokenFactory;

    /// <summary>
    /// The correspondence request to send
    /// </summary>
    public CorrespondenceRequest CorrespondenceRequest => _correspondenceRequest;

    private SendCorrespondencePayload(
        CorrespondenceRequest correspondenceRequest,
        Func<Task<AccessToken>> accessTokenFactory
    )
    {
        _correspondenceRequest = correspondenceRequest;
        _accessTokenFactory = accessTokenFactory;
    }

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
        return new SendCorrespondencePayload(correspondenceRequest, accessTokenFactory);
    }

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
        return new SendCorrespondencePayload(correspondenceRequestBuilder.Build(), accessTokenFactory);
    }
}

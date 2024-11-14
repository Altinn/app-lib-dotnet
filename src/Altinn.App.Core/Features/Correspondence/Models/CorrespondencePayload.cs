using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Payloads required for interaction with the <see cref="CorrespondenceClient"/>
/// </summary>
public static class CorrespondencePayload
{
    /// <summary>
    /// Provides the required authorisation properties which are common for all Correspondence interaction
    /// </summary>
    public abstract record AuthorisationBase
    {
        /// <summary>
        /// Access token factory delegate (e.g. <see cref="MaskinportenClient.GetAltinnExchangedToken"/>)
        /// </summary>
        /// <remarks>
        /// <see cref="CorrespondenceClient.Authorisation"/> provides factories for known authorisation methods, which may be used here
        /// </remarks>
        public required Func<Task<AccessToken>> AccessTokenFactory { get; init; }
    }

    /// <summary>
    /// Represents the payload for sending a correspondence
    /// </summary>
    public sealed record Send : AuthorisationBase
    {
        /// <summary>
        /// The correspondence request to send
        /// </summary>
        public required CorrespondenceRequest CorrespondenceRequest { get; init; }
    }

    /// <summary>
    /// Represents a payload for querying the status of a correspondence
    /// </summary>
    public sealed record Status : AuthorisationBase
    {
        /// <summary>
        /// The correspondence identifier
        /// </summary>
        public required Guid CorrespondenceId { get; init; }
    }
}

using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Features.Correspondence;

/// <summary>
/// Exposes factory methods for known authorisation methods
/// </summary>
public interface ICorrespondenceAuthorisationFactory
{
    /// <summary>
    /// Provides an Altinn exchanged "org token" from the <see cref="IMaskinportenClient"/> implementation
    /// </summary>
    /// <remarks>
    /// Take care to set up <see cref="MaskinportenSettings"/> with a client that can claim the
    /// `altinn:correspondence.write` and `altinn:serviceowner/instances.read` scopes
    /// </remarks>
    Func<Task<AccessToken>> Maskinporten { get; }
}

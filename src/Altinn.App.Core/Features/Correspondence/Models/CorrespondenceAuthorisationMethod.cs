using Altinn.App.Core.Features.Maskinporten;

namespace Altinn.App.Core.Features.Correspondence.Models;

/// <summary>
/// Indicates which built-in authorisation method should be used for <see cref="CorrespondenceClient.Authorisation"/>
/// </summary>
public enum CorrespondenceAuthorisationMethod
{
    /// <summary>
    /// Use <see cref="MaskinportenClient"/> for authorisation
    /// </summary>
    Maskinporten,
}

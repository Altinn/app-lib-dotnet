using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for sending correspondence to alert a signee of a signing call to action.
/// </summary>
internal interface ISigningCallToActionService
{
    /// <summary>
    /// Sends correspondence to a signee to notify them of a signing call to action.
    /// </summary>
    Task<SendCorrespondenceResponse?> SendSignCallToAction(
        Notification? notification,
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        Party signingParty,
        Party serviceOwnerParty,
        List<AltinnEnvironmentConfig>? correspondenceResources
    );
}

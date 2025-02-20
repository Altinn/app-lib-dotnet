using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for sending correspondence for a signing task.
/// </summary>
public interface ISigningCorrespondenceService
{
    /// <summary>
    /// Sends correspondence to a signee after signing action has been completed.
    /// </summary>
    public Task<SendCorrespondenceResponse?> SendSignConfirmationCorrespondence(
        InstanceIdentifier instanceIdentifier,
        Signee signee,
        IEnumerable<DataElementSignature> dataElementSignatures,
        UserActionContext context,
        List<AltinnEnvironmentConfig>? correspondenceResources
    );

    /// <summary>
    /// Sends correspondence to a signee to notify them of a signing call to action.
    /// </summary>
    public Task<SendCorrespondenceResponse?> SendSignCallToActionCorrespondence(
        Notification? notification,
        AppIdentifier appIdentifier,
        InstanceIdentifier instanceIdentifier,
        Party signingParty,
        Party serviceOwnerParty,
        List<AltinnEnvironmentConfig>? correspondenceResources
    );
}

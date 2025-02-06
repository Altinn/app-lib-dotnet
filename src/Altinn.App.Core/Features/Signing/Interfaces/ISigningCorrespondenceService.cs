using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for sending correspondence for a signing task.
/// </summary>
public interface ISigningCorrespondenceService
{
    /// <summary>
    /// Sends correspondence for a signing task.
    /// </summary>
    public Task<SendCorrespondenceResponse?> SendCorrespondence(
        InstanceIdentifier instanceIdentifier,
        Signee signee,
        IEnumerable<DataElementSignature> dataElementSignatures,
        UserActionContext context,
        List<AltinnEnvironmentConfig>? correspondenceResources
    );
}

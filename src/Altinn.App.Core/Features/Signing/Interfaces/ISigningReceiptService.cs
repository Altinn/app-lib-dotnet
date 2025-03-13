using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for sending correspondence receipt for a signing user action.
/// </summary>
public interface ISigningReceiptService
{
    /// <summary>
    /// Sends correspondence to a signee after signing action has been completed.
    /// </summary>
    public Task<SendCorrespondenceResponse?> SendSignatureReceipt(
        InstanceIdentifier instanceIdentifier,
        Signee signee,
        IEnumerable<DataElementSignature> dataElementSignatures,
        UserActionContext context,
        List<AltinnEnvironmentConfig>? correspondenceResources
    );
}

using Altinn.App.Core.Features.Signing.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningDelegationService
{
    internal Task<List<SigneeContext>> DelegateSigneeRights(
        string taskId,
        Instance instance,
        List<SigneeContext> signeeContexts,
        CancellationToken ct
    );
}

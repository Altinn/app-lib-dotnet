using Altinn.App.Core.Features.Signing.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningService
{
    Task<List<SigneeContext>> InitializeSignees(string taskId, CancellationToken ct);

    Task<List<SigneeContext>> ProcessSignees(
        string taskId,
        Instance instance,
        List<SigneeContext> signeeContexts,
        CancellationToken ct
    );

    List<SigneeContext> ReadSignees();
}

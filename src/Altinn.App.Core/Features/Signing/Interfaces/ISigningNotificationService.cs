using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningNotificationService
{
    Task<List<SigneeContext>> NotifySignatureTask(List<SigneeContext> signeeContexts, CancellationToken? ct = null);
}

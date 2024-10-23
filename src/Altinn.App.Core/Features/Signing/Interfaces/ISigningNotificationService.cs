using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningNotificationService
{
    Task NotifySignatureTask(List<SigneeContext> signeeContexts, CancellationToken ct);
}

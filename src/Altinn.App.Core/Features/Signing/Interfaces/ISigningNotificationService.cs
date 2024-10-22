using Altinn.App.Core.Features.Signing.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningNotificationService
{
    Task NotifySignees(List<SigneeContext> signeeContexts, CancellationToken ct);
}

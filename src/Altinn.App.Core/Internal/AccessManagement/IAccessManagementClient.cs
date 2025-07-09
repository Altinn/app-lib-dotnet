using Altinn.App.Core.Internal.AccessManagement.Models;

namespace Altinn.App.Core.Internal.AccessManagement;

internal interface IAccessManagementClient
{
    public Task<DelegationResponse> DelegateRights(DelegationRequest delegation, CancellationToken ct);
    public Task<DelegationResponse> RevokeRights(DelegationRequest delegation, CancellationToken ct);
}

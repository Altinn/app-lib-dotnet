using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Caching;

/// <summary>
/// Cache for Instance and form data scoped to a distributed lock.
/// Cache keys include both lock token and instance GUID for debugging and isolation.
/// </summary>
internal interface ILockScopedInstanceCache
{
    /// <summary>
    /// Try to get cached Instance for the lock scope.
    /// </summary>
    Task<Instance?> GetInstance(string lockToken, Guid instanceGuid, CancellationToken ct = default);

    /// <summary>
    /// Cache Instance for the lock scope.
    /// </summary>
    Task SetInstance(string lockToken, Guid instanceGuid, Instance instance, CancellationToken ct = default);

    /// <summary>
    /// Try to get cached binary data for a data element.
    /// </summary>
    Task<ReadOnlyMemory<byte>?> GetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Cache binary data for a data element.
    /// </summary>
    Task SetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default
    );

    /// <summary>
    /// Remove binary data from cache (e.g., on delete).
    /// </summary>
    Task RemoveBinaryData(string lockToken, Guid instanceGuid, Guid dataElementId, CancellationToken ct = default);

    /// <summary>
    /// Invalidate all cached data for a lock scope. Optional - TTL handles cleanup if not called.
    /// </summary>
    Task InvalidateSession(string lockToken, Guid instanceGuid, CancellationToken ct = default);
}

using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Caching;

/// <summary>
/// Cache for Instance and form data during a processing session (distributed lock scope).
/// </summary>
internal interface IProcessingSessionCache
{
    /// <summary>
    /// Try to get cached Instance for the session.
    /// </summary>
    Task<Instance?> GetInstance(string lockToken, CancellationToken ct = default);

    /// <summary>
    /// Cache Instance for the session.
    /// </summary>
    Task SetInstance(string lockToken, Instance instance, CancellationToken ct = default);

    /// <summary>
    /// Try to get cached binary data for a data element.
    /// </summary>
    Task<ReadOnlyMemory<byte>?> GetBinaryData(string lockToken, Guid dataElementId, CancellationToken ct = default);

    /// <summary>
    /// Cache binary data for a data element.
    /// </summary>
    Task SetBinaryData(string lockToken, Guid dataElementId, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>
    /// Remove binary data from cache (e.g., on delete).
    /// </summary>
    Task RemoveBinaryData(string lockToken, Guid dataElementId, CancellationToken ct = default);

    /// <summary>
    /// Invalidate all cached data for a session. Optional - TTL handles cleanup if not called.
    /// </summary>
    Task InvalidateSession(string lockToken, CancellationToken ct = default);
}

using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Caching;

/// <summary>
/// No-op implementation for requests without a lock scope.
/// </summary>
internal sealed class NullLockScopedInstanceCache : ILockScopedInstanceCache
{
    public static NullLockScopedInstanceCache Instance { get; } = new();

    public Task<Instance?> GetInstance(string lockToken, Guid instanceGuid, CancellationToken ct) =>
        Task.FromResult<Instance?>(null);

    public Task SetInstance(string lockToken, Guid instanceGuid, Instance instance, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<ReadOnlyMemory<byte>?> GetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        CancellationToken ct
    ) => Task.FromResult<ReadOnlyMemory<byte>?>(null);

    public Task SetBinaryData(
        string lockToken,
        Guid instanceGuid,
        Guid dataElementId,
        ReadOnlyMemory<byte> data,
        CancellationToken ct
    ) => Task.CompletedTask;

    public Task RemoveBinaryData(string lockToken, Guid instanceGuid, Guid dataElementId, CancellationToken ct) =>
        Task.CompletedTask;

    public Task InvalidateSession(string lockToken, Guid instanceGuid, CancellationToken ct) => Task.CompletedTask;
}

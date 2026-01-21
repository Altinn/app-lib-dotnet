using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine;

/// <summary>
/// No-op implementation for requests without a processing session.
/// </summary>
internal sealed class NullProcessingSessionCache : IProcessingSessionCache
{
    public static NullProcessingSessionCache Instance { get; } = new();

    public Task<Instance?> GetInstance(string lockToken, CancellationToken ct) => Task.FromResult<Instance?>(null);

    public Task SetInstance(string lockToken, Instance instance, CancellationToken ct) => Task.CompletedTask;

    public Task<ReadOnlyMemory<byte>?> GetBinaryData(string lockToken, Guid dataElementId, CancellationToken ct) =>
        Task.FromResult<ReadOnlyMemory<byte>?>(null);

    public Task SetBinaryData(string lockToken, Guid dataElementId, ReadOnlyMemory<byte> data, CancellationToken ct) =>
        Task.CompletedTask;

    public Task RemoveBinaryData(string lockToken, Guid dataElementId, CancellationToken ct) => Task.CompletedTask;

    public Task InvalidateSession(string lockToken, CancellationToken ct) => Task.CompletedTask;
}

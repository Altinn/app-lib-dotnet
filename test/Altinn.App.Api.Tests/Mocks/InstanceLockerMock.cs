using Altinn.App.Core.Internal.InstanceLocking;

namespace Altinn.App.Api.Tests.Mocks;

internal sealed class InstanceLockerMock : IInstanceLocker
{
    private string? _externalLockToken;

    public IInstanceLock InitLock() => NoOpLock.Instance;

    public IInstanceLock InitLock(int instanceOwnerPartyId, Guid instanceGuid) => NoOpLock.Instance;

    public Task<IInstanceLock> Lock() => Task.FromResult<IInstanceLock>(NoOpLock.Instance);

    public Task<IInstanceLock> Lock(TimeSpan ttl) => Task.FromResult<IInstanceLock>(NoOpLock.Instance);

    public string? CurrentLockToken => _externalLockToken;

    public void UseExternalLockToken(string lockToken) => _externalLockToken = lockToken;

    private sealed class NoOpLock : IInstanceLock
    {
        public static readonly NoOpLock Instance = new();

        public Task Lock(TimeSpan? ttl = null) => Task.CompletedTask;

        public Task UpdateTtl(TimeSpan ttl) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

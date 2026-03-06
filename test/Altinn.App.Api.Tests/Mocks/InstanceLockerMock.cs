using Altinn.App.Core.Internal.InstanceLocking;

namespace Altinn.App.Api.Tests.Mocks;

internal sealed class InstanceLockerMock : IInstanceLocker
{
    private static readonly string _fakeLockToken = Guid.NewGuid().ToString("N");

    public ValueTask<string> LockAsync() => ValueTask.FromResult(_fakeLockToken);

    public ValueTask<string> LockAsync(TimeSpan ttl) => ValueTask.FromResult(_fakeLockToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

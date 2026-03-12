namespace Altinn.App.Core.Internal.InstanceLocking;

internal interface IInstanceLocker : IAsyncDisposable
{
    ValueTask<string> LockAsync();

    ValueTask<string> LockAsync(TimeSpan ttl);

    ValueTask<string> LockAsync(int instanceOwnerPartyId, Guid instanceGuid);

    ValueTask<string> LockAsync(int instanceOwnerPartyId, Guid instanceGuid, TimeSpan ttl);
}

namespace Altinn.App.Core.Internal.InstanceLocking;

internal interface IInstanceLocker : IAsyncDisposable
{
    ValueTask<string> LockAsync();

    ValueTask<string> LockAsync(TimeSpan ttl);
}

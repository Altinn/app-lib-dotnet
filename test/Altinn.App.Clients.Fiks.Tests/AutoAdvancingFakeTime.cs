namespace Altinn.App.Clients.Fiks.Tests;

using Microsoft.Extensions.Time.Testing;

public sealed class AutoAdvancingFakeTime : IAsyncDisposable
{
    private readonly FakeTimeProvider _fakeTime;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _advance;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runner;

    public TimeProvider Provider => _fakeTime;

    private AutoAdvancingFakeTime(TimeSpan interval, TimeSpan advance, DateTimeOffset? startTime = null)
    {
        _fakeTime = new FakeTimeProvider(startTime ?? DateTimeOffset.UtcNow);
        _interval = interval;
        _advance = advance;

        _runner = Task.Run(AutoAdvanceLoop);
    }

    public static AutoAdvancingFakeTime Create(TimeSpan interval, TimeSpan advance, DateTimeOffset? startTime = null) =>
        new(interval, advance, startTime);

    private async Task AutoAdvanceLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            await Task.Delay(_interval, _cts.Token);
            _fakeTime.Advance(_advance);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        await CastAndDispose(_cts);
        await CastAndDispose(_runner);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}

namespace Altinn.App.ProcessEngine;

internal sealed class BooleanBuffer(int maxSize = 10) : IDisposable
{
    private readonly Queue<bool> _queue = new(maxSize);
    private readonly SemaphoreSlim _lock = new(1, 1);

    private async Task ExecuteLocked(Action action)
    {
        await _lock.WaitAsync();
        try
        {
            action();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<T> ExecuteLocked<T>(Func<T> func)
    {
        await _lock.WaitAsync();
        try
        {
            return func();
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task Add(bool value) =>
        ExecuteLocked(() =>
        {
            if (_queue.Count >= maxSize)
                _queue.Dequeue();

            _queue.Enqueue(value);
        });

    public Task Clear() => ExecuteLocked(() => _queue.Clear());

    public Task<bool?> Latest() => ExecuteLocked<bool?>(() => _queue.LastOrDefault());

    public Task<bool?> Previous() => ExecuteLocked<bool?>(() => _queue.ElementAtOrDefault(_queue.Count - 2));

    public Task<int> ConsecutiveFalseCount() =>
        ExecuteLocked(() =>
        {
            int count = 0;
            foreach (var value in _queue.Reverse())
            {
                if (!value)
                    count++;
                else
                    break;
            }

            return count;
        });

    public void Dispose()
    {
        _lock.Dispose();
    }
}

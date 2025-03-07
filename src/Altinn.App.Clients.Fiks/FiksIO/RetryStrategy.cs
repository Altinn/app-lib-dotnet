namespace Altinn.App.Clients.Fiks.FiksIO;

// TODO: Replace with Polly
public sealed class RetryStrategy
{
    public IReadOnlyList<TimeSpan> Intervals { get; }

    private RetryStrategy(IEnumerable<TimeSpan> intervals)
    {
        Intervals = intervals as IReadOnlyList<TimeSpan> ?? intervals.ToList();
    }

    public static RetryStrategy None { get; } = new([TimeSpan.Zero]);

    public static RetryStrategy Default { get; } = Exponential(3);

    public static RetryStrategy Exponential(int numberOfRetries) =>
        new(Enumerable.Range(0, numberOfRetries).Select(i => TimeSpan.FromSeconds(Math.Pow(2, i))));

    public static RetryStrategy Custom(IEnumerable<TimeSpan> intervals) => new(intervals);

    public static RetryStrategy Custom(int numberOfRetries, TimeSpan interval) =>
        new(Enumerable.Repeat(interval, numberOfRetries));

    public async Task<TResult> Execute<TResult>(
        Func<Task<TResult>> action,
        Action<Exception, TimeSpan>? errorHandler = null
    )
    {
        var intervals = Intervals.Any() ? Intervals : [TimeSpan.Zero];
        Exception? lastException = null;

        for (int i = 0; i <= intervals.Count; i++)
        {
            try
            {
                return await action.Invoke();
            }
            catch (Exception e)
            {
                lastException = e;

                if (i < intervals.Count)
                {
                    errorHandler?.Invoke(e, intervals[i]);
                    await Task.Delay(intervals[i]);
                }
            }
        }

        throw new Exception(
            $"Failed execution after {intervals.Count} attempts: {lastException?.Message}",
            lastException
        );
    }
}

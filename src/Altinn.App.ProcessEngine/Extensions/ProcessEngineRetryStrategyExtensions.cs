using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Extensions;

internal static class ProcessEngineRetryStrategyExtensions
{
    public static bool CanRetry(this ProcessEngineRetryStrategy strategy, int iteration) =>
        !strategy.MaxRetries.HasValue || strategy.MaxRetries < iteration;

    public static TimeSpan CalculateDelay(this ProcessEngineRetryStrategy strategy, int iteration)
    {
        var delay = strategy.BackoffType switch
        {
            ProcessEngineBackoffType.Constant => strategy.Delay,
            ProcessEngineBackoffType.Linear => TimeSpan.FromSeconds(strategy.Delay.TotalSeconds * iteration),
            ProcessEngineBackoffType.Exponential => TimeSpan.FromSeconds(
                strategy.Delay.TotalSeconds * Math.Pow(2, iteration - 2)
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
        };

        return delay > strategy.MaxDelay ? strategy.MaxDelay.Value : delay;
    }
}

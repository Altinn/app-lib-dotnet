using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.App.ProcessEngine.Data;

internal sealed class ProcessEnginePgRepository : IProcessEngineRepository
{
    private readonly ProcessEngineDbContext _context;
    private readonly ProcessEngineRetryStrategy _retryStrategy;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessEnginePgRepository> _logger;

    public ProcessEnginePgRepository(
        ProcessEngineDbContext context,
        ProcessEngineRetryStrategy retryStrategy,
        TimeProvider timeProvider,
        ILogger<ProcessEnginePgRepository> logger
    )
    {
        _context = context;
        _retryStrategy = retryStrategy;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProcessEngineJob>> GetIncompleteJobs(
        CancellationToken cancellationToken = default
    ) =>
        await ExecuteWithRetry(
            async ct =>
            {
                var incompleteStatuses = new[]
                {
                    ProcessEngineItemStatus.Enqueued,
                    ProcessEngineItemStatus.Processing,
                    ProcessEngineItemStatus.Requeued,
                };

                var jobs = await _context
                    .Jobs.Include(j => j.Tasks)
                    .Where(j => incompleteStatuses.Contains(j.Status))
                    .ToListAsync(ct);

                return jobs;
            },
            cancellationToken
        );

    public async Task SaveJob(ProcessEngineJob job, CancellationToken cancellationToken = default) =>
        await ExecuteWithRetry(
            async ct =>
            {
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync(ct);
            },
            cancellationToken
        );

    public async Task UpdateJob(ProcessEngineJob job, CancellationToken cancellationToken = default) =>
        await ExecuteWithRetry(
            async ct =>
            {
                _context.Jobs.Update(job);
                await _context.SaveChangesAsync(ct);
            },
            cancellationToken
        );

    public async Task UpdateTask(ProcessEngineTask task, CancellationToken cancellationToken = default) =>
        await ExecuteWithRetry(
            async ct =>
            {
                _context.Tasks.Update(task);
                await _context.SaveChangesAsync(ct);
            },
            cancellationToken
        );

    private async Task ExecuteWithRetry(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string operationName = ""
    ) =>
        await ExecuteWithRetry<object?>(
            async ct =>
            {
                await operation(ct);
                return null;
            },
            cancellationToken,
            operationName
        );

    private async Task<T> ExecuteWithRetry<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string operationName = ""
    )
    {
        var attempt = 1;

        while (true)
        {
            try
            {
                var result = await operation(cancellationToken);

                if (attempt > 1)
                {
                    _logger.LogDebug(
                        "Database operation '{OperationName}' succeeded on attempt {Attempt}",
                        operationName,
                        attempt
                    );
                }

                return result; // Success
            }
            catch (Exception ex) when (ShouldRetry(ex))
            {
                if (!_retryStrategy.CanRetry(attempt))
                {
                    _logger.LogError(
                        ex,
                        "Database operation '{OperationName}' failed permanently after {Attempts} attempts",
                        operationName,
                        attempt
                    );
                    throw; // Give up after max retries
                }

                var delay = _retryStrategy.CalculateDelay(attempt);

                _logger.LogWarning(
                    ex,
                    "Database operation '{OperationName}' failed on attempt {Attempt}, retrying in {Delay}ms",
                    operationName,
                    attempt,
                    delay.TotalMilliseconds
                );

                await Task.Delay(delay, _timeProvider, cancellationToken);
                attempt++;
            }
        }
    }

    private static bool ShouldRetry(Exception exception) =>
        exception switch
        {
            // Network/connection issues - retryable
            TimeoutException => true,
            SocketException => true,
            HttpRequestException => true,

            // Database-specific transient errors - retryable
            _ when exception.GetType().Name.Contains("Timeout") => true,
            _ when exception.GetType().Name.Contains("Connection") => true,
            _ when exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) => true,
            _ when exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) => true,

            // Permanent errors - don't retry
            ArgumentNullException => false,
            ArgumentException => false,
            InvalidOperationException => false,

            // Default to retrying for unknown exceptions (conservative approach)
            _ => true,
        };
}

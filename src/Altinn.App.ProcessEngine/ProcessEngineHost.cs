using Altinn.App.ProcessEngine.Exceptions;
using Altinn.App.ProcessEngine.Extensions;

namespace Altinn.App.ProcessEngine;

internal sealed class ProcessEngineHost(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly IProcessEngine _processEngine = serviceProvider.GetRequiredService<IProcessEngine>();
    private readonly ILogger<ProcessEngineHost> _logger = serviceProvider.GetRequiredService<
        ILogger<ProcessEngineHost>
    >();

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting process engine.");
        await _processEngine.Start(stoppingToken);
        _logger.LogInformation("Process engine initialized.");

        int failCount = 0;
        int maxFailsAllowed = 100;
        var healthCheckInterval = TimeSpan.FromSeconds(10);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(healthCheckInterval, stoppingToken);
            var status = _processEngine.Status;

            if (status.HasFullQueue())
                _logger.LogWarning(
                    "Process engine has backpressure, processing queue is full. Current status: {HealthStatus}",
                    status
                );

            if (status.IsDisabled() || status.IsHealthy())
            {
                if (failCount > 0)
                    _logger.LogInformation(
                        "Process engine has recovered and is healthy. Current status: {HealthStatus}",
                        status
                    );

                failCount = 0;
            }
            else
            {
                _logger.LogWarning("Process engine is unhealthy. Current status: {HealthStatus}", status);

                failCount++;
                if (failCount >= maxFailsAllowed)
                {
                    _logger.LogCritical(
                        "The process engine has failed {FailCount} times. Shutting down host.",
                        failCount
                    );
                    throw new ProcessEngineCriticalException(
                        "Critical failure in ProcessEngineHost. Forcing application shutdown."
                    );
                }
            }
        }

        _logger.LogInformation("Process engine host shutting down.");
    }
}

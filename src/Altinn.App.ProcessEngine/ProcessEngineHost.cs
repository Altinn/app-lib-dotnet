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

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Process engine host cancellation requested.");
        }
    }
}

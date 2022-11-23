using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Altinn.App.Core.Internal.AppValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Altinn.App.Core.Configuration;


namespace Altinn.App.Core.Internal.AppFiles;

/// <summary>
/// Hosted service that can be registrerd in order to read updated json files
/// </summary>
public class AppFilesSyncHostedServiceDebug : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AppFilesSyncHostedServiceDebug> _logger;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    public AppFilesSyncHostedServiceDebug(IServiceScopeFactory serviceScopeFactory, IOptions<AppSettings> appSettings, ILogger<AppFilesSyncHostedServiceDebug> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        var path = string.IsNullOrWhiteSpace(appSettings.Value.AppBasePath) ? Directory.GetCurrentDirectory() : appSettings.Value.AppBasePath;
        _fileWatcher = new FileSystemWatcher(path);
        _semaphore = new SemaphoreSlim(1);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var isValid = true;
        try
        {
            var errors = await ValidateFiles();
            if (errors.Count > 0)
            {
                AppValidationError.PrintErors(errors);
                isValid = false;
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "AppValidation failed");
            isValid = false;
        }

        if (!isValid)
        {
            //Ensure that everything gets written before exiting
            Console.Out.Flush();
            await Task.Delay(1000);
            System.Environment.Exit(-1);

        }
        await Task.Delay(100000);

        _logger.LogInformation($"Starting {nameof(AppFilesSyncHostedServiceDebug)}");
        _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        _fileWatcher.IncludeSubdirectories = true;
        _fileWatcher.Filters.Add("*.json");
        _fileWatcher.Filters.Add("*.xml");
        _fileWatcher.Filters.Add("*.bpmn");
        _fileWatcher.Created += OnFileChange;
        _fileWatcher.Changed += OnFileChange;
        _fileWatcher.Deleted += OnFileChange;
        _fileWatcher.Renamed += OnFileChange;
        _fileWatcher.EnableRaisingEvents = true;

    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Stopping {nameof(AppFilesSyncHostedServiceDebug)}");
        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Dispose();
        return Task.CompletedTask;
    }

    private int _waitingThreads = 0;

    private async void OnFileChange(object sender, FileSystemEventArgs args)
    {
        _logger.LogTrace("Detected {changeType} on {filename}", args.ChangeType, args.Name);

        try
        {
            // Ensure that we don't run multiple validations simultaniously
            // Some file write operations might happen in separate steps, and we get two events
            Interlocked.Increment(ref _waitingThreads);
            await Task.Delay(50);
            await _semaphore.WaitAsync();
            if (_waitingThreads > 1)
            {
                return; // There are other waiting threads, so release the semaphore and let the next event validate the app.
            }

            _logger.LogInformation("Detected {changeType} on {filename}", args.ChangeType, args.Name);
            var errors = await ValidateFiles();

            if (errors.Count == 0)
            {
                Console.WriteLine("Validation OK");
            }
            else
            {
                AppValidationError.PrintErors(errors);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Handeling file change failed");
        }
        finally
        {
            Interlocked.Decrement(ref _waitingThreads);
            _semaphore.Release();
        }
    }

    private async Task<List<AppValidationError>> ValidateFiles()
    {
        await using (var sp = _serviceScopeFactory.CreateAsyncScope())
        {
            var appFilesLoader = sp.ServiceProvider.GetRequiredService<AppFilesLoader>();
            var appValidator = sp.ServiceProvider.GetRequiredService<AppValidator>();
            var appFiles = await appFilesLoader.GetAppFilesBytes();
            return await appValidator.Validate(appFiles);
        }
    }
}
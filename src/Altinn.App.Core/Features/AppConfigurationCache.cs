using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features;

internal static class AppConfigurationCacheDI
{
    public static IServiceCollection AddAppConfigurationCache(this IServiceCollection services)
    {
        services.AddSingleton<AppConfigurationCache>();
        services.AddSingleton<IAppConfigurationCache>(sp => sp.GetRequiredService<AppConfigurationCache>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<AppConfigurationCache>());
        return services;
    }
}

internal interface IAppConfigurationCache
{
    public ApplicationMetadata ApplicationMetadata { get; }
}

internal sealed class AppConfigurationCache(
    ILogger<AppConfigurationCache> logger,
    IServiceProvider serviceProvider,
    IConfiguration configuration
) : BackgroundService, IAppConfigurationCache
{
    private readonly ILogger<AppConfigurationCache> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IConfiguration _configuration = configuration;

    private ApplicationMetadata? _appMetadata;

    public ApplicationMetadata ApplicationMetadata =>
        _appMetadata ?? throw new InvalidOperationException("Cache not initialized");

    private readonly TaskCompletionSource _firstTick = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        if (_configuration.GetValue<bool>("GeneralSettings:DisableAppConfigurationCache"))
            return;
        await _firstTick.Task;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_configuration.GetValue<bool>("GeneralSettings:DisableAppConfigurationCache"))
            return;
        try
        {
            var env = _serviceProvider.GetRequiredService<IHostEnvironment>();
            var appMetadata = _serviceProvider.GetRequiredService<IAppMetadata>();
            if (env.IsDevelopment())
            {
                // local dev, config can change
                {
                    await using var scope = await Scope.Create(_serviceProvider);
                    await UpdateCache(this, scope, stoppingToken);
                }

                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await using var scope = await Scope.Create(_serviceProvider);
                    await UpdateCache(this, scope, stoppingToken);
                }
            }
            else if (env.IsStaging())
            {
                // tt02 (container deployment, immutable infra)
                await using var scope = await Scope.Create(_serviceProvider);
                await UpdateCache(this, scope, stoppingToken);
            }
            else if (env.IsProduction())
            {
                // prod (container deployment, immutable infra)
                await using var scope = await Scope.Create(_serviceProvider);
                await UpdateCache(this, scope, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _firstTick.TrySetCanceled(stoppingToken);
        }
        catch (Exception ex)
        {
            _firstTick.TrySetException(ex);
            _logger.LogError(ex, "Error starting AppConfigurationCache");
        }

        static async ValueTask UpdateCache(AppConfigurationCache self, Scope scope, CancellationToken cancellationToken)
        {
            self._appMetadata = await scope.AppMetadata.GetApplicationMetadata();

            self._firstTick.TrySetResult();
        }
    }

    private readonly record struct Scope(AsyncServiceScope Value, IAppMetadata AppMetadata) : IAsyncDisposable
    {
        public static async ValueTask<Scope> Create(IServiceProvider serviceProvider)
        {
            var scope = serviceProvider.CreateAsyncScope();
            try
            {
                var appMetadata = serviceProvider.GetRequiredService<IAppMetadata>();
                return new Scope(scope, appMetadata);
            }
            catch
            {
                await scope.DisposeAsync();
                throw;
            }
        }

        public ValueTask DisposeAsync() => Value.DisposeAsync();
    }
}

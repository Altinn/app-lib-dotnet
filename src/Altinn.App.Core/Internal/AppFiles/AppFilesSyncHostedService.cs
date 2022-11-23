using Microsoft.Extensions.Hosting;

namespace Altinn.App.Core.Internal.AppFiles;

/// <summary>
/// Call <see cref="AppFilesLoader.GetAppFilesBytes" /> on startup
/// </summary>
public class AppFilesSyncHostedService : IHostedService
{
    private readonly AppFilesLoader _appFilesLoader;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    public AppFilesSyncHostedService(AppFilesLoader appFilesLoader)
    {
        _appFilesLoader = appFilesLoader;
    }

    /// <summary>
    /// Call <see cref="AppFilesLoader.GetAppFilesBytes" /> on startup
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var appFiles = await _appFilesLoader.GetAppFilesBytes();
        AppFilesBytes.Bytes = appFiles;
    }

    /// <summary>
    /// Required by interface
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
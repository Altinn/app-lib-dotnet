using System.Globalization;
using System.Net;
using Altinn.App.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal;

internal static class LocaltestClientDI
{
    public static IServiceCollection AddLocaltestClient(this IServiceCollection services)
    {
        services.AddSingleton<LocaltestClient>();
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<LocaltestClient>());
        return services;
    }
}

internal sealed class LocaltestClient : BackgroundService
{
    private const string ExpectedHostname = "local.altinn.cloud";

    private readonly ILogger<LocaltestClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<GeneralSettings> _generalSettings;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly TimeProvider _timeProvider;
    private readonly TaskCompletionSource<VersionResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal Task<VersionResult> FirstResult => _tcs.Task;

    internal VersionResult? Result;

    public LocaltestClient(
        ILogger<LocaltestClient> logger,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<GeneralSettings> generalSettings,
        IHostApplicationLifetime lifetime,
        TimeProvider? timeProvider = null
    )
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _generalSettings = generalSettings;
        _lifetime = lifetime;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuredHostname = _generalSettings.CurrentValue.HostName;
        if (configuredHostname != ExpectedHostname)
            return;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await Version();
                Result = result;
                _tcs.TrySetResult(result);
                switch (result)
                {
                    case VersionResult.Ok { Version: var version }:
                    {
                        _logger.LogInformation("Localtest version: {Version}", version);
                        if (version >= 1)
                            return;
                        _logger.LogError(
                            "Localtest version is not supported for this version of the app backend. Update your local copy of localtest."
                                + " Version found: '{Version}'. Shutting down..",
                            version
                        );
                        _lifetime.StopApplication();
                        return;
                    }
                    case VersionResult.ApiNotFound:
                    {
                        _logger.LogError(
                            "Localtest version may be outdated, as we failed to probe {HostName} API for version information."
                                + "Is localtest running on {HostName}? Do you have a recent copy of localtest? Shutting down..",
                            ExpectedHostname,
                            ExpectedHostname
                        );
                        _lifetime.StopApplication();
                        return;
                    }
                    case VersionResult.ApiNotAvailable { Error: var error }:
                        _logger.LogWarning(
                            "Localtest API could not be reached, is it running? Trying again soon.. Error: {Error}",
                            error
                        );
                        break;
                    case VersionResult.UnhandledStatusCode { StatusCode: var statusCode }:
                        _logger.LogError(
                            "Localtest version endpoint returned unexpected status code: {StatusCode}",
                            statusCode
                        );
                        break;
                    case VersionResult.UnknownError { Exception: var ex }:
                        _logger.LogError(ex, "Error while trying fetching localtest version");
                        break;
                    case VersionResult.AppShuttingDown:
                        return;
                }
                await Task.Delay(TimeSpan.FromSeconds(5), _timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) { }
    }

    internal abstract record VersionResult
    {
        // Localtest is running, and we got a version number, which means this is a version of localtest that has
        // the new version endpoint.
        public sealed record Ok(int Version) : VersionResult;

        public sealed record InvalidVersionResponse(string Repsonse) : VersionResult;

        // Whatever listened on "local.altinn.cloud:80" responded with a 404
        public sealed record ApiNotFound() : VersionResult;

        // The request timed out. Note that there may be multiple variants of timeouts.
        public sealed record Timeout() : VersionResult;

        // Could not connect to "local.altinn.cloud:80", a server might not be listening on that address
        // or it might be a network issue
        public sealed record ApiNotAvailable(HttpRequestError Error) : VersionResult;

        // Request was cancelled because the application is shutting down
        public sealed record AppShuttingDown() : VersionResult;

        // The localtest endpoint returned an unexpected statuscode
        public sealed record UnhandledStatusCode(HttpStatusCode StatusCode) : VersionResult;

        // Unhandled error
        public sealed record UnknownError(Exception Exception) : VersionResult;
    }

    private async Task<VersionResult> Version()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5), _timeProvider);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, _lifetime.ApplicationStopping);
        var cancellationToken = linkedCts.Token;
        try
        {
            using var client = _httpClientFactory.CreateClient();

            var baseUrl = _generalSettings.CurrentValue.LocaltestUrl;
            var url = $"{baseUrl}/Home/Localtest/Version";

            using var response = await client.GetAsync(url, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var versionStr = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!int.TryParse(versionStr, CultureInfo.InvariantCulture, out var version))
                        return new VersionResult.InvalidVersionResponse(versionStr);
                    return new VersionResult.Ok(version);
                case HttpStatusCode.NotFound:
                    return new VersionResult.ApiNotFound();
                default:
                    return new VersionResult.UnhandledStatusCode(response.StatusCode);
            }
        }
        catch (OperationCanceledException)
        {
            if (_lifetime.ApplicationStopping.IsCancellationRequested)
                return new VersionResult.AppShuttingDown();

            return new VersionResult.Timeout();
        }
        catch (HttpRequestException ex)
        {
            if (_lifetime.ApplicationStopping.IsCancellationRequested)
                return new VersionResult.AppShuttingDown();

            return new VersionResult.ApiNotAvailable(ex.HttpRequestError);
        }
        catch (Exception ex)
        {
            return new VersionResult.UnknownError(ex);
        }
    }
}

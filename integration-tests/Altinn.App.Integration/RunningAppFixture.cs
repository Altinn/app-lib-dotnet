using System.Runtime.CompilerServices;
using DotNet.Testcontainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Altinn.App.Integration;

public abstract class RunningAppFixture : IAsyncLifetime
{
    private const ushort LocaltestPort = 5101;
    private const ushort AppPort = 5005;
    private const string LocaltestHostname = "localtest";
    private const string AppHostname = "app";
    private static readonly string _localtestUrl = $"http://{LocaltestHostname}:{LocaltestPort}";
    private const bool ReuseContainers = false;
    private const bool CleanUpContainers = true;

    private readonly INetwork _network;
    private readonly IFutureDockerImage _appBuilder;
    private readonly IContainer _localtestContainer;
    private readonly IContainer _appContainer;

    private HttpClient? _client;

    private readonly Dictionary<string, string> _localtestEnv = new()
    {
        { "DOTNET_ENVIRONMENT", "Docker" },
        { "LocalPlatformSettings__LocalAppUrl", $"http://{AppHostname}:{AppPort}" },
    };

    private readonly Dictionary<string, string> _appEnv = new()
    {
        { "DOTNET_ENVIRONMENT", "Development" },
        { "AppSettings__OpenIdWellKnownEndpoint", $"{_localtestUrl}/authentication/api/v1/openid/" },
        { "PlatformSettings__ApiStorageEndpoint", $"{_localtestUrl}/storage/api/v1/" },
        { "PlatformSettings__ApiRegisterEndpoint", $"{_localtestUrl}/register/api/v1/" },
        { "PlatformSettings__ApiProfileEndpoint", $"{_localtestUrl}/profile/api/v1/" },
        { "PlatformSettings__ApiAuthenticationEndpoint", $"{_localtestUrl}/authentication/api/v1/" },
        { "PlatformSettings__ApiAuthorizationEndpoint", $"{_localtestUrl}/authorization/api/v1/" },
        { "PlatformSettings__ApiEventsEndpoint", $"{_localtestUrl}/events/api/v1/" },
        // { "PlatformSettings__ApiPdf2Endpoint", "http://localhost:5300/pdf" },
        { "PlatformSettings__ApiNotificationEndpoint", $"{_localtestUrl}/notifications/api/v1/" },
        { "PlatformSettings__ApiCorrespondenceEndpoint", $"{_localtestUrl}/correspondence/api/v1/" },
    };

    protected RunningAppFixture(string appName, IMessageSink messageSink)
    {
        var logger = new MessageSinkLogger(messageSink);
        _network = new NetworkBuilder()
            .WithCleanUp(CleanUpContainers)
            .WithReuse(ReuseContainers)
            .WithLogger(logger)
            .Build();

        _localtestContainer = new ContainerBuilder()
            .WithImage(new DockerImage("localtest:latest"))
            .WithEnvironment(_localtestEnv)
            .WithHostname(LocaltestHostname)
            .WithPortBinding(LocaltestPort, true)
            .WithNetwork(_network)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
            .WithCleanUp(CleanUpContainers)
            .WithReuse(ReuseContainers)
            .WithLogger(logger)
            .Build();

        var appsDirectory = GetAndPrepareAppsDirectory();
        _appBuilder = new ImageFromDockerfileBuilder()
            .WithName($"{appName}-integration:latest")
            .WithDockerfileDirectory(appsDirectory)
            .WithBuildArgument("APP_FOLDER", appName)
            .WithCleanUp(false)
            .WithLogger(logger)
            .Build();

        _appContainer = new ContainerBuilder()
            .WithImage(_appBuilder)
            .WithHostname(AppHostname)
            .WithEnvironment(_appEnv)
            .WithPortBinding(AppPort, assignRandomHostPort: true)
            .DependsOn(_localtestContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
            .WithNetwork(_network)
            .WithReuse(ReuseContainers)
            .WithCleanUp(CleanUpContainers)
            .WithLogger(logger)
            .Build();
    }

    private static string GetAndPrepareAppsDirectory()
    {
        var integrationTestsDirectory = Path.GetDirectoryName(GetCallerDirectory());
        var rootDirectory = Path.GetDirectoryName(integrationTestsDirectory);
        var appsDirectory = Path.Join(integrationTestsDirectory, "Apps");
        var appLibPackagesDirectory = Path.Join(appsDirectory, "app-lib-packages");
        Directory.Delete(appLibPackagesDirectory, true);
        Directory.CreateDirectory(appLibPackagesDirectory);

        string[] searchDirectories =
        [
            Path.Join(rootDirectory, "src", "Altinn.App.Core", "bin", "Debug"),
            Path.Join(rootDirectory, "src", "Altinn.App.Api", "bin", "Debug"),
        ];

        var latestPackages = searchDirectories
            .Select(dir =>
                Directory
                    .GetFiles(dir, "*.nupkg", SearchOption.TopDirectoryOnly)
                    .Select(file => new FileInfo(file))
                    .OrderByDescending(file => file.LastWriteTime)
                    .FirstOrDefault()
            )
            .Where(file => file != null);

        foreach (var latestPackage in latestPackages)
        {
            ArgumentNullException.ThrowIfNull(latestPackage);

            string destinationPath = Path.Join(appLibPackagesDirectory, latestPackage.Name);

            File.Copy(latestPackage.FullName, destinationPath);
            Console.WriteLine($"Copied: {latestPackage.Name}");
        }

        return appsDirectory;
    }

    private static string GetCallerDirectory([CallerFilePath] string? callerFilePath = null)
    {
        var directory = Path.GetDirectoryName(callerFilePath);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        return directory;
    }

    public async Task InitializeAsync()
    {
        await _appBuilder.CreateAsync();
        await _localtestContainer.StartAsync();
        await _appContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _appContainer.DisposeAsync();
        await _appBuilder.DisposeAsync();
        await _localtestContainer.DisposeAsync();
        await _network.DisposeAsync();
    }

    public HttpClient GetAppClient()
    {
        if (_client == null)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(
                    $"http://local.altinn.cloud:{_localtestContainer.GetMappedPublicPort(LocaltestPort)}"
                ),
            };
        }

        return _client;
    }
}

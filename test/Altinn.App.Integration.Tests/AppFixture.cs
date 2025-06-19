using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CliWrap;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Altinn.App.Integration.Tests;

public sealed partial class AppFixture : IAsyncDisposable
{
    private const ushort LocaltestPort = 5101;
    private const ushort AppPort = 5005;
    private const string LocaltestHostname = "localtest";
    private const string AppHostname = "app";
    private static readonly string _localtestUrl = $"http://{LocaltestHostname}:{LocaltestPort}";
    private const bool ReuseContainers = false;
    private const bool CleanUpContainers = true;

    private static long _fixtureInstance = -1;
    private static readonly SemaphoreSlim _buildLock = new(1, 1);
    private static IFutureDockerImage? _localtestContainerImage;
    private static Dictionary<string, IFutureDockerImage> _appContainerImages = new();

    private static long NextFixtureInstance() => Interlocked.Increment(ref _fixtureInstance);

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly TestOutputLogger _logger;
    private readonly string _app;
    private readonly INetwork _network;
    private readonly IContainer _localtestContainer;
    private readonly IContainer _appContainer;

    private HttpClient? _appClient;
    private HttpClient? _localtestClient;

    private static readonly Dictionary<string, string> _localtestEnv = new()
    {
        { "DOTNET_ENVIRONMENT", "Docker" },
        { "LocalPlatformSettings__LocalAppUrl", $"http://{AppHostname}:{AppPort}" },
    };

    private static Dictionary<string, string?> CreateAppEnv() =>
        new()
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

    private AppFixture(
        TestOutputLogger logger,
        string app,
        INetwork network,
        IContainer localtestContainer,
        IContainer appContainer
    )
    {
        _logger = logger;
        _app = app;
        _network = network;
        _localtestContainer = localtestContainer;
        _appContainer = appContainer;
    }

    public static async Task<AppFixture> Create(ITestOutputHelper output, string name)
    {
        var fixtureInstance = NextFixtureInstance();
        var testContainersLogger = new TestOutputLogger(output, fixtureInstance, name, forTestContainers: true);
        var logger = new TestOutputLogger(output, fixtureInstance, name, forTestContainers: false);
        var appDirectory = GetAppDir(name);
        var solutionDirectory = GetSolutionDir();
        IFutureDockerImage? localtestContainerImage = null;
        IFutureDockerImage? appContainerImage = null;
        await _buildLock.WaitAsync();
        try
        {
            localtestContainerImage = _localtestContainerImage;
            if (localtestContainerImage is null)
            {
                logger.LogInformation("Packing applib");
                await PackLibraries();

                logger.LogInformation("Building container images");
                _localtestContainerImage = localtestContainerImage = new ImageFromDockerfileBuilder()
                    .WithName($"applib-localtest:latest")
                    .WithDockerfileDirectory(Path.Join(solutionDirectory, "app-localtest"))
                    .WithCleanUp(false)
                    .WithLogger(testContainersLogger)
                    .Build();

                await localtestContainerImage.CreateAsync();
            }

            if (!_appContainerImages.TryGetValue(name, out appContainerImage))
            {
                logger.LogInformation("Building app container image");

                CopyPackages(name);

                _appContainerImages[name] = appContainerImage = new ImageFromDockerfileBuilder()
                    .WithName($"applib-{name}:latest")
                    .WithDockerfileDirectory(Directory.GetParent(appDirectory)!.FullName)
                    .WithCleanUp(false)
                    .WithLogger(testContainersLogger)
                    .Build();
                await appContainerImage.CreateAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build");
            throw;
        }
        finally
        {
            _buildLock.Release();
        }
        Assert.NotNull(localtestContainerImage);
        Assert.NotNull(appContainerImage);

        INetwork? network = null;
        IContainer? localtestContainer = null;
        IContainer? appContainer = null;
        try
        {
            logger.LogInformation("Starting fixture");
            network = new NetworkBuilder()
                .WithName($"applib-{name}-network-{fixtureInstance:00}")
                .WithCleanUp(CleanUpContainers)
                .WithReuse(ReuseContainers)
                .WithLogger(testContainersLogger)
                .Build();

            await network.CreateAsync();

            localtestContainer = new ContainerBuilder()
                .WithName($"applib-{name}-localtest-{fixtureInstance:00}")
                .WithImage(localtestContainerImage)
                .WithEnvironment(_localtestEnv)
                .WithHostname(LocaltestHostname)
                .WithPortBinding(LocaltestPort, true)
                .WithNetwork(network)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
                .WithCleanUp(CleanUpContainers)
                .WithReuse(ReuseContainers)
                .WithLogger(testContainersLogger)
                .Build();

            await localtestContainer.StartAsync();

            var appEnv = CreateAppEnv();
            appContainer = new ContainerBuilder()
                .WithName($"applib-{name}-app-{fixtureInstance:00}")
                .WithImage(appContainerImage)
                .WithHostname(AppHostname)
                .WithEnvironment(appEnv)
                .WithPortBinding(AppPort, assignRandomHostPort: true)
                .DependsOn(localtestContainer)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilContainerIsHealthy())
                .WithNetwork(network)
                .WithReuse(ReuseContainers)
                .WithCleanUp(CleanUpContainers)
                .WithLogger(testContainersLogger)
                .Build();

            await appContainer.StartAsync();

            return new AppFixture(logger, name, network, localtestContainer, appContainer);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Failed to create fixture");

            await TryDispose(logger, appContainer);
            await TryDispose(logger, localtestContainer);
            await TryDispose(logger, network);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_localtestClient is not null)
        {
            _localtestClient.Dispose();
            _localtestClient = null;
        }
        if (_appClient is not null)
        {
            _appClient.Dispose();
            _appClient = null;
        }

        await TryDispose(_appContainer);
        await TryDispose(_localtestContainer);
        await TryDispose(_network);
    }

    private async Task TryDispose(IAsyncDisposable? disposable) => await TryDispose(_logger, disposable);

    private static async Task TryDispose(TestOutputLogger logger, IAsyncDisposable? disposable)
    {
        if (disposable is null)
            return;

        try
        {
            await disposable.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispose {Type}", disposable.GetType().FullName);
        }
    }

    public HttpClient GetAppClient()
    {
        if (_appClient == null)
        {
            _appClient = new HttpClient
            {
                BaseAddress = new Uri(
                    $"http://local.altinn.cloud:{_localtestContainer.GetMappedPublicPort(LocaltestPort)}"
                ),
            };
        }

        return _appClient;
    }

    public HttpClient GetLocaltestClient()
    {
        if (_localtestClient == null)
        {
            _localtestClient = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{_localtestContainer.GetMappedPublicPort(LocaltestPort)}"),
            };
        }

        return _localtestClient;
    }

    internal record ApiResponse(AppFixture Fixture, HttpResponseMessage Response) : IDisposable
    {
        protected virtual SettingsTask ConfigureVerify(SettingsTask settings) => settings;

        public async Task<T?> Read<T>()
        {
            var content = await Response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, _jsonSerializerOptions);
        }

        public SettingsTask Verify(Func<string, string>? replacer = null, [CallerFilePath] string sourceFile = "")
        {
            var appPort = Fixture._appContainer.GetMappedPublicPort(AppPort).ToString();
            var localtestPort = Fixture._localtestContainer.GetMappedPublicPort(LocaltestPort).ToString();
            var settings = Verifier
                .Verify(Response, sourceFile: sourceFile)
                .AddExtraSettings(settings =>
                {
                    settings.Converters.Add(new HeadersConverter(appPort, localtestPort, replacer));
                    settings.Converters.Add(new UriConverter(appPort, localtestPort, replacer));
                });
            return ConfigureVerify(settings);
        }

        public SettingsTask Verify<T>(
            T readResponse,
            Func<string, string>? replacer = null,
            [CallerFilePath] string sourceFile = ""
        )
        {
            var appPort = Fixture._appContainer.GetMappedPublicPort(AppPort).ToString();
            var localtestPort = Fixture._localtestContainer.GetMappedPublicPort(LocaltestPort).ToString();
            var settings = Verifier
                .Verify(new { Response = readResponse, HttpResponse = Response }, sourceFile: sourceFile)
                .AddExtraSettings(settings =>
                {
                    settings.Converters.Add(new HeadersConverter(appPort, localtestPort, replacer));
                    settings.Converters.Add(new UriConverter(appPort, localtestPort, replacer));
                });
            return ConfigureVerify(settings);
        }

        public void Dispose() => Response.Dispose();
    }

    private sealed class HeadersConverter(string appPort, string localtestPort, Func<string, string>? replacer)
        : WriteOnlyJsonConverter<HttpHeaders>
    {
        private readonly string _appPort = appPort;
        private readonly string _localtestPort = localtestPort;
        private readonly Func<string, string>? _replacer = replacer;

        public override void Write(VerifyJsonWriter writer, HttpHeaders value)
        {
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);

                switch (kvp.Key)
                {
                    case "Date":
                    case "Authorization":
                        writer.WriteValue("{Scrubbed}");
                        break;
                    default:
                        writer.WriteStartArray();
                        foreach (var headerValue in kvp.Value)
                        {
                            string v = headerValue;
                            if (_replacer is not null)
                                v = _replacer(v);
                            v = v.Replace(_appPort, "APP_PORT");
                            v = v.Replace(_localtestPort, "LOCALTEST_PORT");
                            writer.WriteValue(v);
                        }
                        writer.WriteEndArray();
                        break;
                }
            }
            writer.WriteEndObject();
        }
    }

    private sealed class UriConverter(string appPort, string localtestPort, Func<string, string>? replacer)
        : WriteOnlyJsonConverter<Uri>
    {
        private readonly string _appPort = appPort;
        private readonly string _localtestPort = localtestPort;
        private readonly Func<string, string>? _replacer = replacer;

        public override void Write(VerifyJsonWriter writer, Uri value)
        {
            var uri = value.ToString();
            if (_replacer is not null)
                uri = _replacer(uri);
            uri = uri.Replace(_appPort, "APP_PORT");
            uri = uri.Replace(_localtestPort, "LOCALTEST_PORT");
            writer.WriteValue(uri);
        }
    }

    private static string GetAppDir(string name)
    {
        var integrationTestsDirectory = GetCallerDirectory();
        var appDirectory = Path.Join(integrationTestsDirectory, $"testapps/{name}/App");
        var info = new DirectoryInfo(appDirectory);
        if (!info.Exists)
        {
            throw new DirectoryNotFoundException(
                $"The directory {appDirectory} does not exist. Please check the path."
            );
        }
        return info.FullName;
    }

    private static async Task PackLibraries()
    {
        var integrationTestsDirectory = GetCallerDirectory();
        var output = Path.Join(integrationTestsDirectory, "testapps", "_packages");
        var solutionDirectory = GetSolutionDir();
        var result = await Cli.Wrap("dotnet")
            .WithArguments(["pack", "-c", "Release", "--output", output])
            .WithWorkingDirectory(solutionDirectory)
            .ExecuteAsync();
        Assert.Equal(0, result.ExitCode);
    }

    private static void CopyPackages(string name)
    {
        var appDirectory = GetAppDir(name);
        var integrationTestsDirectory = GetCallerDirectory();
        var packagesDirectory = Path.Join(integrationTestsDirectory, "testapps", "_packages");
        var appPackagesDirectory = Path.Join(appDirectory, "..", "_packages");
        if (Directory.Exists(appPackagesDirectory))
        {
            Directory.Delete(appPackagesDirectory, true);
        }
        Directory.CreateDirectory(appPackagesDirectory);
        foreach (var file in Directory.GetFiles(packagesDirectory))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(appPackagesDirectory, fileName);
            File.Copy(file, destFile);
        }
    }

    private static string GetSolutionDir()
    {
        var integrationTestsDirectory = GetCallerDirectory();
        var solutionDirectory = Path.Join(integrationTestsDirectory, "..", "..");
        var info = new DirectoryInfo(solutionDirectory);
        if (!info.Exists)
        {
            throw new DirectoryNotFoundException(
                $"The directory {solutionDirectory} does not exist. Please check the path."
            );
        }
        return info.FullName;
    }

    private static string GetCallerDirectory([CallerFilePath] string? callerFilePath = null)
    {
        var directory = Path.GetDirectoryName(callerFilePath);
        ArgumentException.ThrowIfNullOrEmpty(directory);
        return directory;
    }
}

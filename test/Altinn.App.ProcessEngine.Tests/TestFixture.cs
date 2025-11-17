using Altinn.App.ProcessEngine.Extensions;
using Altinn.App.ProcessEngine.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.ProcessEngine.Tests;

internal sealed record TestFixture(
    WebApplication App,
    Mock<IHostEnvironment> HostEnvironmentMock,
    Mock<ILoggerFactory> LoggerFactoryMock,
    Mock<IHttpClientFactory> HttpClientFactoryMock,
    Mock<HttpMessageHandler> HttpMessageHandlerMock
) : IAsyncDisposable
{
    public ProcessEngine ProcessEngine => (ProcessEngine)App.Services.GetRequiredService<IProcessEngine>();
    public ProcessEngineTaskHandler ProcessEngineTaskHandler =>
        (ProcessEngineTaskHandler)App.Services.GetRequiredService<IProcessEngineTaskHandler>();
    public ProcessEngineHost ProcessEngineHost =>
        App.Services.GetServices<IHostedService>().OfType<ProcessEngineHost>().Single();
    public ProcessEngineSettings ProcessEngineSettings =>
        App.Services.GetRequiredService<IOptions<ProcessEngineSettings>>().Value;
    public IHttpClientFactory HttpClientFactory => App.Services.GetRequiredService<IHttpClientFactory>();

    /// <summary>
    /// Creates a new test fixture instance
    /// </summary>
    public static TestFixture Create(
        Action<WebApplicationBuilder>? configure = null,
        string hostEnvironment = "Development",
        bool autoAddProcessEngine = true
    )
    {
        var builder = WebApplication.CreateBuilder();

        if (autoAddProcessEngine)
            builder.Services.AddProcessEngine();

        configure?.Invoke(builder);

        // Mocks
        var hostEnvironmentMock = new Mock<IHostEnvironment>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(httpMessageHandlerMock.Object));
        hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns(hostEnvironment);
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        builder.Services.AddSingleton(hostEnvironmentMock.Object);
        builder.Services.AddSingleton(loggerFactoryMock.Object);
        builder.Services.AddSingleton(httpClientFactoryMock.Object);

        return new TestFixture(
            builder.Build(),
            hostEnvironmentMock,
            loggerFactoryMock,
            httpClientFactoryMock,
            httpMessageHandlerMock
        );
    }

    public async ValueTask DisposeAsync()
    {
        await ProcessEngine.Stop();
        ProcessEngine.Dispose();
        await App.DisposeAsync();
    }
}

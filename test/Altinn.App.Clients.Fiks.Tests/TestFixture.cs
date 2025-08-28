using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Extensions;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Common.AccessTokenClient.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly;

namespace Altinn.App.Clients.Fiks.Tests;

/// <summary>
/// Test fixture for FiksIO and FiksArkiv.
/// Sets up mocks for external dependencies, but not for FiksIO/Arkiv implementations.
/// <p>Use the <see cref="Action{IServiceCollection}"/> delegate in the <see cref="Create"/> metod to register
/// <see cref="Fiks.Extensions.ServiceCollectionExtensions.AddFiksIOClient"/> or <see cref="Fiks.Extensions.ServiceCollectionExtensions.AddFiksArkiv"/>
/// along with any additional services and mocks.</p>
/// </summary>
internal sealed record TestFixture(
    WebApplication App,
    Mock<IHostEnvironment> HostEnvironmentMock,
    Mock<IAppMetadata> AppMetadataMock,
    Mock<IMaskinportenClient> MaskinportenClientMock,
    Mock<ILoggerFactory> LoggerFactoryMock,
    Mock<IDataClient> DataClientMock,
    Mock<IInstanceClient> InstanceClientMock,
    Mock<IAppResources> AppResourcesMock,
    Mock<IAppModel> AppModelMock,
    Mock<IAuthenticationContext> AuthenticationContextMock,
    Mock<IAltinnPartyClient> PartyClientMock,
    Mock<ILayoutEvaluatorStateInitializer> LayoutStateInitializerMock,
    Mock<IEmailNotificationClient> EmailNotificationClientMock,
    Mock<IProcessReader> ProcessReaderMock,
    Mock<IHttpClientFactory> HttpClientFactoryMock,
    Mock<IAccessTokenGenerator> AccessTokenGeneratorMock
) : IAsyncDisposable
{
    public IFiksIOClient FiksIOClient => App.Services.GetRequiredService<IFiksIOClient>();
    public FiksIOSettings FiksIOSettings => App.Services.GetRequiredService<IOptions<FiksIOSettings>>().Value;
    public FiksArkivSettings FiksArkivSettings => App.Services.GetRequiredService<IOptions<FiksArkivSettings>>().Value;
    public MaskinportenSettings MaskinportenSettings =>
        App.Services.GetRequiredService<IOptions<MaskinportenSettings>>().Value;
    public FiksArkivConfigValidationService FiksArkivConfigValidationService =>
        App.Services.GetServices<IHostedService>().OfType<FiksArkivConfigValidationService>().Single();
    public FiksArkivEventService FiksArkivEventService =>
        App.Services.GetServices<IHostedService>().OfType<FiksArkivEventService>().Single();
    public IAltinnCdnClient AltinnCdnClient => App.Services.GetRequiredService<IAltinnCdnClient>();
    public IFiksArkivMessageHandler FiksArkivMessageHandler =>
        App.Services.GetRequiredService<IFiksArkivMessageHandler>();
    public IFiksArkivAutoSendDecision FiksArkivAutoSendDecisionHandler =>
        App.Services.GetRequiredService<IFiksArkivAutoSendDecision>();
    public IFiksArkivInstanceClient FiksArkivInstanceClient =>
        App.Services.GetRequiredService<IFiksArkivInstanceClient>();
    public IServiceTask FiksArkivServiceTask =>
        AppImplementationFactory.GetAll<IServiceTask>().First(x => x is IFiksArkivServiceTask);
    public ResiliencePipeline<FiksIOMessageResponse> FiksIOResiliencePipeline =>
        App.Services.ResolveResiliencePipeline();
    public IFiksIOClientFactory FiksIOClientFactory => App.Services.GetRequiredService<IFiksIOClientFactory>();
    public IProcessReader ProcessReader => App.Services.GetRequiredService<IProcessReader>();
    public IHttpClientFactory HttpClientFactory => App.Services.GetRequiredService<IHttpClientFactory>();
    public IAccessTokenGenerator AccessTokenGenerator => App.Services.GetRequiredService<IAccessTokenGenerator>();
    public IAppMetadata AppMetadata => App.Services.GetRequiredService<IAppMetadata>();
    public AppImplementationFactory AppImplementationFactory =>
        App.Services.GetRequiredService<AppImplementationFactory>();

    private static JsonSerializerOptions _jsonSerializerOptions =>
        new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

    /// <summary>
    /// Creates a new test fixture instance
    /// </summary>
    public static TestFixture Create(
        Action<IServiceCollection> configureServices,
        IEnumerable<(string, object)>? configurationCollection = null,
        bool useDefaultFiksIOSettings = true,
        bool useDefaultFiksArkivSettings = true,
        bool useDefaultMaskinportenSettings = true
    )
    {
        var builder = WebApplication.CreateBuilder();

        // Default configuration values
        Dictionary<string, object> config = new();
        if (useDefaultFiksIOSettings)
        {
            config["FiksIOSettings"] = TestHelpers.GetDefaultFiksIOSettings();
        }
        if (useDefaultFiksArkivSettings)
        {
            config["FiksArkivSettings"] = TestHelpers.GetDefaultFiksArkivSettings();
        }
        if (useDefaultMaskinportenSettings)
        {
            config["MaskinportenSettings"] = TestHelpers.GetDefaultMaskinportenSettings();
            builder.Services.ConfigureMaskinportenClient("MaskinportenSettings");
        }

        builder.Configuration.AddJsonStream(GetJsonStream(config));

        // User supplied configuration values
        if (configurationCollection is not null)
        {
            foreach (var (configName, configValue) in configurationCollection)
            {
                builder.Configuration.AddJsonStream(
                    GetJsonStream(new Dictionary<string, object> { [configName] = configValue })
                );
            }
        }

        // User-supplied services configuration
        configureServices(builder.Services);

        // Mocks
        var hostEnvironmentMock = new Mock<IHostEnvironment>();
        var appMetadataMock = new Mock<IAppMetadata>();
        var maskinportenClientMock = new Mock<IMaskinportenClient>();
        var dataClientMock = new Mock<IDataClient>();
        var instanceClientMock = new Mock<IInstanceClient>();
        var appResourcesMock = new Mock<IAppResources>();
        var appModelMock = new Mock<IAppModel>();
        var authenticationContextMock = new Mock<IAuthenticationContext>();
        var partyClientMock = new Mock<IAltinnPartyClient>();
        var layoutStateInitializerMock = new Mock<ILayoutEvaluatorStateInitializer>();
        var emailNotificationClientMock = new Mock<IEmailNotificationClient>();
        var processReaderMock = new Mock<IProcessReader>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var accessTokenGeneratorMock = new Mock<IAccessTokenGenerator>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        hostEnvironmentMock.Setup(x => x.EnvironmentName).Returns("Development");
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());
        appMetadataMock
            .Setup(x => x.GetApplicationMetadata())
            .ReturnsAsync(new ApplicationMetadata("ttd/unit-testing"));

        builder.Services.AddSingleton(hostEnvironmentMock.Object);
        builder.Services.AddSingleton(appMetadataMock.Object);
        builder.Services.AddSingleton(maskinportenClientMock.Object);
        builder.Services.AddSingleton(loggerFactoryMock.Object);
        builder.Services.AddSingleton(dataClientMock.Object);
        builder.Services.AddSingleton(authenticationContextMock.Object);
        builder.Services.AddSingleton(partyClientMock.Object);
        builder.Services.AddSingleton(layoutStateInitializerMock.Object);
        builder.Services.AddSingleton(emailNotificationClientMock.Object);
        builder.Services.AddSingleton(appResourcesMock.Object);
        builder.Services.AddSingleton(instanceClientMock.Object);
        builder.Services.AddSingleton(appModelMock.Object);
        builder.Services.AddSingleton(processReaderMock.Object);
        builder.Services.AddSingleton(httpClientFactoryMock.Object);
        builder.Services.AddSingleton(accessTokenGeneratorMock.Object);

        // Non-mockable services
        builder.Services.AddSingleton<IAuthenticationTokenResolver, AuthenticationTokenResolver>();
        builder.Services.AddTransient<InstanceDataUnitOfWorkInitializer>();
        builder.Services.AddSingleton<ModelSerializationService>();
        builder.Services.AddAppImplementationFactory();

        return new TestFixture(
            builder.Build(),
            hostEnvironmentMock,
            appMetadataMock,
            maskinportenClientMock,
            loggerFactoryMock,
            dataClientMock,
            instanceClientMock,
            appResourcesMock,
            appModelMock,
            authenticationContextMock,
            partyClientMock,
            layoutStateInitializerMock,
            emailNotificationClientMock,
            processReaderMock,
            httpClientFactoryMock,
            accessTokenGeneratorMock
        );
    }

    private static Stream GetJsonStream(IDictionary<string, object> data)
    {
        var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
    }
}

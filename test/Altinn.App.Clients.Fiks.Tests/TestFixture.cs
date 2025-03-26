using System.Text;
using System.Text.Json;
using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Registry;

namespace Altinn.App.Clients.Fiks.Tests;

internal sealed record TestFixture(
    WebApplication App,
    Mock<IWebHostEnvironment> WebHostEnvironmentMock,
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
    Mock<IEmailNotificationClient> EmailNotificationClientMock
) : IAsyncDisposable, IDisposable
{
    public IFiksIOClient FiksIOClient => App.Services.GetRequiredService<IFiksIOClient>();
    public FiksIOSettings FiksIOSettings => App.Services.GetRequiredService<IOptions<FiksIOSettings>>().Value;
    public FiksArkivSettings FiksArkivSettings => App.Services.GetRequiredService<IOptions<FiksArkivSettings>>().Value;
    public FiksArkivConfigValidationService FiksArkivConfigValidationService =>
        App.Services.GetServices<IHostedService>().OfType<FiksArkivConfigValidationService>().Single();
    public FiksArkivEventService FiksArkivEventService =>
        App.Services.GetServices<IHostedService>().OfType<FiksArkivEventService>().Single();
    public IAltinnCdnClient AltinnCdnClient => App.Services.GetRequiredService<IAltinnCdnClient>();
    public IFiksArkivMessageHandler FiksArkivMessageHandler =>
        App.Services.GetRequiredService<IFiksArkivMessageHandler>();
    public IFiksArkivServiceTask FiksArkivServiceTask => App.Services.GetRequiredService<IFiksArkivServiceTask>();
    public ResiliencePipeline<FiksIOMessageResponse> FiksIOResiliencePipeline =>
        App.Services.GetKeyedService<ResiliencePipeline<FiksIOMessageResponse>>(FiksIOConstants.ResiliencePipelineId)
        ?? App.Services.GetRequiredKeyedService<ResiliencePipeline<FiksIOMessageResponse>>(
            FiksIOConstants.DefaultResiliencePipelineId
        );

    public static TestFixture Create(
        Action<IServiceCollection> configureServices,
        IDictionary<string, object>? configurationValues = null
    )
    {
        var builder = WebApplication.CreateBuilder();
        configureServices(builder.Services);

        // Default configuration values
        Dictionary<string, object> config = new()
        {
            ["FiksIOSettings"] = GetDefaultFiksIOSettings(),
            ["FiksArkivSettings"] = GetDefaultFiksArkivSettings(),
        };

        builder.Configuration.AddJsonStream(GetJsonStream(config));

        // User supplied configuration values
        if (configurationValues is not null)
        {
            builder.Configuration.AddJsonStream(GetJsonStream(configurationValues));
        }

        // Mocks
        var webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
        // var hostEnvironmentMock = new Mock<IHostEnvironment>();
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
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        builder.Services.AddSingleton(webHostEnvironmentMock.Object);
        builder.Services.AddSingleton(appMetadataMock.Object);
        builder.Services.AddSingleton(maskinportenClientMock.Object);
        builder.Services.AddSingleton(loggerFactoryMock.Object);
        builder.Services.AddSingleton(dataClientMock.Object);
        builder.Services.AddSingleton(authenticationContextMock.Object);
        builder.Services.AddSingleton(partyClientMock.Object);
        builder.Services.AddSingleton(layoutStateInitializerMock.Object);
        builder.Services.AddSingleton(emailNotificationClientMock.Object);
        // builder.Services.AddSingleton(hostEnvironmentMock.Object);
        builder.Services.AddSingleton(appResourcesMock.Object);
        builder.Services.AddSingleton(instanceClientMock.Object);
        builder.Services.AddSingleton(appModelMock.Object);

        // Non-mockable services
        builder.Services.AddTransient<InstanceDataUnitOfWorkInitializer>();
        builder.Services.AddSingleton<ModelSerializationService>();

        return new TestFixture(
            builder.Build(),
            webHostEnvironmentMock,
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
            emailNotificationClientMock
        );
    }

    private static Stream GetJsonStream(IDictionary<string, object> data)
    {
        var json = JsonSerializer.Serialize(data);
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    // public static MaskinportenSettings GetDefaultMaskinportenSettings()
    // {
    //     return new MaskinportenSettings
    //     {
    //         Authority = "test-authority",
    //         ClientId = "test-client-id",
    //         JwkBase64 = "test-jwk-base64",
    //     };
    // }

    public static FiksIOSettings GetDefaultFiksIOSettings()
    {
        return new FiksIOSettings
        {
            AccountId = Guid.Empty,
            IntegrationId = Guid.Empty,
            IntegrationPassword = "test-integration-password",
            AccountPrivateKeyBase64 = "test-account-pk-base64",
            AsicePrivateKeyBase64 = "test-asice-pk-base64",
        };
    }

    public static FiksArkivSettings GetDefaultFiksArkivSettings()
    {
        return new FiksArkivSettings
        {
            ErrorHandling = new FiksArkivErrorHandlingSettings
            {
                SendEmailNotifications = true,
                EmailNotificationRecipients = ["recipient@example.com"],
            },
            AutoSend = new FiksArkivAutoSendSettings
            {
                AfterTaskId = "Task_1",
                Recipient = new FiksArkivRecipientSettings { AccountId = Guid.Empty },
                PrimaryDocument = new FiksArkivPayloadSettings
                {
                    DataType = "ref-data-as-pdf",
                    Filename = "formdata.pdf",
                },
                Attachments =
                [
                    new FiksArkivPayloadSettings { DataType = "model", Filename = "formdata.xml" },
                    new FiksArkivPayloadSettings { DataType = "uploaded_attachment" },
                ],
            },
        };
    }

    public static FiksIOSettings GetRandomFiksIOSettings()
    {
        return new FiksIOSettings
        {
            AccountId = Guid.NewGuid(),
            IntegrationId = Guid.NewGuid(),
            IntegrationPassword = Guid.NewGuid().ToString(),
            AccountPrivateKeyBase64 = Guid.NewGuid().ToString(),
            AsicePrivateKeyBase64 = Guid.NewGuid().ToString(),
        };
    }

    public static FiksArkivSettings GetRandomFiksArkivSettings()
    {
        return new FiksArkivSettings
        {
            ErrorHandling = new FiksArkivErrorHandlingSettings
            {
                SendEmailNotifications = true,
                EmailNotificationRecipients = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()],
            },
            AutoSend = new FiksArkivAutoSendSettings
            {
                AfterTaskId = Guid.NewGuid().ToString(),
                Recipient = new FiksArkivRecipientSettings
                {
                    AccountId = Guid.NewGuid(),
                    DataModelBinding = new FiksArkivDataModelBindingSettings
                    {
                        DataType = Guid.NewGuid().ToString(),
                        Field = Guid.NewGuid().ToString(),
                    },
                },
                PrimaryDocument = new FiksArkivPayloadSettings
                {
                    DataType = Guid.NewGuid().ToString(),
                    Filename = Guid.NewGuid().ToString(),
                },
                Attachments =
                [
                    new FiksArkivPayloadSettings
                    {
                        DataType = Guid.NewGuid().ToString(),
                        Filename = Guid.NewGuid().ToString(),
                    },
                ],
            },
        };
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
    }

    public void Dispose()
    {
        ((IDisposable)App).Dispose();
    }

    public class CustomFiksArkivMessageHandler : IFiksArkivMessageHandler
    {
        public Task ValidateConfiguration()
        {
            throw new NotImplementedException();
        }

        public Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
        {
            throw new NotImplementedException();
        }

        public Task HandleReceivedMessage(Instance instance, FiksIOReceivedMessage receivedMessage)
        {
            throw new NotImplementedException();
        }
    }
}

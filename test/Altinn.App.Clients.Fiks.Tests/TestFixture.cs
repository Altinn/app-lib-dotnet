using System.Text;
using System.Text.Json;
using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Internal.App;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Registry;

namespace Altinn.App.Clients.Fiks.Tests;

public sealed record TestFixture(
    WebApplication App,
    Mock<IWebHostEnvironment> WebHostEnvironmentMock,
    Mock<IAppMetadata> AppMetadataMock,
    Mock<IMaskinportenClient> MaskinportenClientMock,
    Mock<ILoggerFactory> LoggerFactoryMock
) : IAsyncDisposable, IDisposable
{
    public IFiksIOClient FiksIOClient => App.Services.GetRequiredService<IFiksIOClient>();
    public FiksIOSettings FiksIOSettings => App.Services.GetRequiredService<IOptions<FiksIOSettings>>().Value;
    public ResiliencePipeline<FiksIOMessageResponse> FiksIOResiliencePipeline =>
        App.Services.GetRequiredKeyedService<ResiliencePipeline<FiksIOMessageResponse>>(
            FiksIOConstants.ResiliencePipelineId
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
        var appMetadataMock = new Mock<IAppMetadata>();
        var maskinportenClientMock = new Mock<IMaskinportenClient>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        builder.Services.AddSingleton(webHostEnvironmentMock.Object);
        builder.Services.AddSingleton(appMetadataMock.Object);
        builder.Services.AddSingleton(maskinportenClientMock.Object);
        builder.Services.AddSingleton(loggerFactoryMock.Object);

        return new TestFixture(
            builder.Build(),
            webHostEnvironmentMock,
            appMetadataMock,
            maskinportenClientMock,
            loggerFactoryMock
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

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
    }

    public void Dispose()
    {
        ((IDisposable)App).Dispose();
    }
}

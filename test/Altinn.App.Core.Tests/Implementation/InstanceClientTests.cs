#nullable disable
using System.Net;
using System.Text;
using Altinn.App.Common.Tests;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace Altinn.App.PlatformServices.Tests.Implementation;

public sealed class InstanceClientTests : IDisposable
{
    private readonly Mock<IOptions<PlatformSettings>> platformSettingsOptions;
    private readonly Mock<IOptionsMonitor<AppSettings>> appSettingsOptions;
    private readonly Mock<HttpMessageHandler> handlerMock;
    private readonly Mock<IHttpContextAccessor> contextAccessor;
    private readonly Mock<ILogger<InstanceClient>> logger;
    private readonly TelemetrySink telemetry;

    public InstanceClientTests()
    {
        platformSettingsOptions = new Mock<IOptions<PlatformSettings>>();
        appSettingsOptions = new Mock<IOptionsMonitor<AppSettings>>();
        handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        contextAccessor = new Mock<IHttpContextAccessor>();
        logger = new Mock<ILogger<InstanceClient>>();
        telemetry = new TelemetrySink();
    }

    [Fact]
    public async Task AddCompleteConfirmation_SuccessfulCallToStorage()
    {
        // Arrange
        Instance instance = new Instance
        {
            CompleteConfirmations = new List<CompleteConfirmation>
            {
                new CompleteConfirmation { StakeholderId = "test" }
            }
        };

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(instance), Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["complete"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        // Act
        await target.AddCompleteConfirmation(1337, Guid.NewGuid());

        // Assert
        handlerMock.VerifyAll();

        await Verify(telemetry.GetSnapshot());
    }

    [Fact]
    public async Task AddCompleteConfirmation_StorageReturnsNonSuccess_ThrowsPlatformHttpException()
    {
        // Arrange
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = new StringContent("Error message", Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["complete"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        PlatformHttpException actualException = null;

        // Act
        try
        {
            await target.AddCompleteConfirmation(1337, Guid.NewGuid());
        }
        catch (PlatformHttpException e)
        {
            actualException = e;
        }

        // Assert
        handlerMock.VerifyAll();

        Assert.NotNull(actualException);
    }

    [Fact]
    public async Task UpdateReadStatus_StorageReturnsNonSuccess_LogsErrorAppContinues()
    {
        // Arrange
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = new StringContent("Error message", Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["read"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        PlatformHttpException actualException = null;

        // Act
        try
        {
            await target.UpdateReadStatus(1337, Guid.NewGuid(), "read");
        }
        catch (PlatformHttpException e)
        {
            actualException = e;
        }

        // Assert
        handlerMock.VerifyAll();

        Assert.Null(actualException);
    }

    [Fact]
    public async Task UpdateReadStatus_StorageReturnsSuccess()
    {
        // Arrange
        Instance expected = new Instance { Status = new InstanceStatus { ReadStatus = ReadStatus.Read } };

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["read"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        // Act
        Instance actual = await target.UpdateReadStatus(1337, Guid.NewGuid(), "read");

        // Assert
        Assert.Equal(expected.Status.ReadStatus, actual.Status.ReadStatus);
        handlerMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateSubtatus_StorageReturnsSuccess()
    {
        // Arrange
        Instance expected = new Instance
        {
            Status = new InstanceStatus
            {
                Substatus = new Substatus { Label = "Substatus.Label", Description = "Substatus.Description" }
            }
        };

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["substatus"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        // Act
        Instance actual = await target.UpdateSubstatus(
            1337,
            Guid.NewGuid(),
            new Substatus { Label = "Substatus.Label", Description = "Substatus.Description" }
        );

        // Assert
        Assert.Equal(expected.Status.Substatus.Label, actual.Status.Substatus.Label);
        Assert.Equal(expected.Status.Substatus.Description, actual.Status.Substatus.Description);
        handlerMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateSubtatus_StorageReturnsNonSuccess_ThrowsPlatformHttpException()
    {
        // Arrange
        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = new StringContent("Error message", Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["substatus"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        PlatformHttpException actualException = null;

        // Act
        try
        {
            await target.UpdateSubstatus(1337, Guid.NewGuid(), new Substatus());
        }
        catch (PlatformHttpException e)
        {
            actualException = e;
        }

        // Assert
        handlerMock.VerifyAll();

        Assert.NotNull(actualException);
    }

    [Fact]
    public async Task DeleteInstance_StorageReturnsSuccess()
    {
        // Arrange
        Guid instanceGuid = Guid.NewGuid();
        string instanceOwnerId = "1337";

        Instance expected = new Instance
        {
            InstanceOwner = new InstanceOwner { PartyId = instanceOwnerId },
            Id = $"{instanceOwnerId}/{instanceGuid}"
        };

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["1337"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        // Act
        Instance actual = await target.DeleteInstance(1337, Guid.NewGuid(), false);

        // Assert
        Assert.Equal("1337", actual.InstanceOwner.PartyId);
        handlerMock.VerifyAll();
    }

    [Fact]
    public async Task DeleteInstance_StorageReturnsNonSuccess_ThrowsPlatformHttpException()
    {
        // Arrange
        Guid instanceGuid = Guid.NewGuid();

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = new StringContent("Error message", Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["1337"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        PlatformHttpException actualException = null;

        // Act
        try
        {
            await target.DeleteInstance(1337, instanceGuid, false);
        }
        catch (PlatformHttpException e)
        {
            actualException = e;
        }

        // Assert
        handlerMock.VerifyAll();

        Assert.NotNull(actualException);
    }

    [Fact]
    public async Task UpdatePresentationTexts_StorageReturnsNonSuccess_ThrowsPlatformHttpException()
    {
        // Arrange
        Guid instanceGuid = Guid.NewGuid();

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.Forbidden,
            Content = new StringContent("Error message", Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["1337"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        PlatformHttpException actualException = null;

        // Act
        try
        {
            await target.UpdatePresentationTexts(1337, instanceGuid, new PresentationTexts());
        }
        catch (PlatformHttpException e)
        {
            actualException = e;
        }

        // Assert
        handlerMock.VerifyAll();

        Assert.NotNull(actualException);
    }

    [Fact]
    public async Task UpdatePresentationTexts_SuccessfulCallToStorage()
    {
        // Arrange
        Guid instanceGuid = Guid.NewGuid();
        int instanceOwnerId = 1337;

        Instance expected = new Instance
        {
            InstanceOwner = new InstanceOwner { PartyId = instanceOwnerId.ToString() },
            Id = $"{instanceOwnerId}/{instanceGuid}"
        };

        HttpResponseMessage httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(expected), Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage], ["presentationtexts"]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        // Act
        await target.UpdatePresentationTexts(instanceOwnerId, instanceGuid, new PresentationTexts());

        // Assert
        handlerMock.VerifyAll();
    }

    [Fact]
    public async Task QueryInstances_QueryResponseContainsNext()
    {
        // Arrange
        QueryResponse<Instance> queryResponse1 =
            new()
            {
                Count = 1,
                Instances = new List<Instance> { new Instance { Id = $"{1337}/{Guid.NewGuid()}" } },
                Next =
                    "https://platform.altinn.no/storage/api/instances?appId=ttd%2Fapps-test&instanceOwner.partyId=1337&status.isArchived=false&status.isSoftDeleted=false&continuationtoken=abcd"
            };

        QueryResponse<Instance> queryResponse2 =
            new()
            {
                Count = 1,
                Instances = new List<Instance> { new Instance { Id = $"{1337}/{Guid.NewGuid()}" } }
            };

        string urlPart1 =
            "instances?appId=ttd%2Fapps-test&instanceOwner.partyId=1337&status.isArchived=false&status.isSoftDeleted=false";
        string urlPart2 =
            "https://platform.altinn.no/storage/api/instances?appId=ttd%2Fapps-test&instanceOwner.partyId=1337&status.isArchived=false&status.isSoftDeleted=false&continuationtoken=abcd";

        HttpResponseMessage httpResponseMessage1 = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(queryResponse1), Encoding.UTF8, "application/json"),
        };

        HttpResponseMessage httpResponseMessage2 = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonConvert.SerializeObject(queryResponse2), Encoding.UTF8, "application/json"),
        };

        InitializeMocks([httpResponseMessage1, httpResponseMessage2], [urlPart1, urlPart2]);

        HttpClient httpClient = new HttpClient(handlerMock.Object);

        InstanceClient target = new InstanceClient(
            platformSettingsOptions.Object,
            logger.Object,
            contextAccessor.Object,
            httpClient,
            appSettingsOptions.Object,
            telemetry.Object
        );

        Dictionary<string, StringValues> queryParams =
            new()
            {
                { "appId", $"ttd/apps-test" },
                { "instanceOwner.partyId", "1337" },
                { "status.isArchived", "false" },
                { "status.isSoftDeleted", "false" }
            };

        // Act
        List<Instance> instances = await target.GetInstances(queryParams);

        // Assert
        Assert.Equal(2, instances.Count);
        handlerMock.VerifyAll();
    }

    private void InitializeMocks(HttpResponseMessage[] httpResponseMessages, string[] urlPart)
    {
        PlatformSettings platformSettings = new PlatformSettings
        {
            ApiStorageEndpoint = "http://localhost",
            SubscriptionKey = "key"
        };
        platformSettingsOptions.Setup(s => s.Value).Returns(platformSettings);

        AppSettings appSettings = new AppSettings { RuntimeCookieName = "AltinnStudioRuntime" };
        appSettingsOptions.Setup(s => s.CurrentValue).Returns(appSettings);

        contextAccessor.Setup(s => s.HttpContext).Returns(new DefaultHttpContext());

        if (httpResponseMessages.Length == 2)
        {
            handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p =>
                        p.RequestUri.ToString().Contains(urlPart[0]) || p.RequestUri.ToString().Contains(urlPart[1])
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(httpResponseMessages[0])
                .ReturnsAsync(httpResponseMessages[1]);
        }
        else
        {
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(p => p.RequestUri.ToString().Contains(urlPart[0])),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(httpResponseMessages[0])
                .Verifiable();
        }
    }

    public void Dispose()
    {
        telemetry.Dispose();
    }
}

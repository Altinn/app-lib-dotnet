using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Moq;

namespace Altinn.App.Clients.Fiks.Tests.FiksArkiv;

public class FiksArkivInstanceClientTest
{
    private const string MaskinportenToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
    private const string LocaltestToken = "localtest-token";
    private readonly InstanceIdentifier _defaultInstanceIdentifier = new($"12345/{Guid.NewGuid()}");

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task GetServiceOwnerAccessToken_ReturnsCorrectToken_BasedOnEnvironment(string environmentName)
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var maskinportenResponse = JwtToken.Parse(MaskinportenToken);
        var appMetadata = await fixture.AppMetadata.GetApplicationMetadata();

        List<HttpRequestMessage> requests = [];
        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(
            HttpStatusCode.OK,
            contentFactory: request => IsTokenRequest(request) ? LocaltestToken : null,
            requestCallback: request => requests.Add(request)
        );

        fixture
            .MaskinportenClientMock.Setup(x =>
                x.GetAltinnExchangedToken(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(maskinportenResponse);
        fixture.HostEnvironmentMock.Setup(x => x.EnvironmentName).Returns(environmentName);
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await fixture.FiksArkivInstanceClient.GetServiceOwnerAccessToken();

        // Assert
        if (environmentName == "Development")
        {
            Assert.Equal(LocaltestToken, result);
            Assert.Single(requests);
            Assert.Equal(HttpMethod.Get, requests[0].Method);
            Assert.Equal(
                $"http://localhost:5101/Home/GetTestOrgToken?org={appMetadata.Org}&orgNumber=991825827&authenticationLevel=3&scopes=altinn%3Aserviceowner%2Finstances.read+altinn%3Aserviceowner%2Finstances.write",
                requests[0].RequestUri!.ToString()
            );
        }
        else
        {
            Assert.Equal(MaskinportenToken, result);
            Assert.Empty(requests);
        }
    }

    [Fact]
    public async Task GetInstance_ReturnsInstance_ForValidResponse()
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var appMetadata = await fixture.AppMetadata.GetApplicationMetadata();
        var instance = new Instance { Id = _defaultInstanceIdentifier.ToString() };

        List<HttpRequestMessage> requests = [];
        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(
            HttpStatusCode.OK,
            contentFactory: request => IsTokenRequest(request) ? LocaltestToken : JsonSerializer.Serialize(instance),
            requestCallback: request => requests.Add(request)
        );
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await fixture.FiksArkivInstanceClient.GetInstance(_defaultInstanceIdentifier);

        // Assert
        Assert.Equal(instance.Id, result.Id);

        HttpRequestMessage instanceRequest = requests.Last();
        Assert.Equal(instanceRequest.Method, HttpMethod.Get);
        Assert.Equal($"Bearer {LocaltestToken}", instanceRequest.Headers.Authorization!.ToString());
        Assert.Equal(
            $"http://local.altinn.cloud/{appMetadata.AppIdentifier}/instances/{_defaultInstanceIdentifier}",
            instanceRequest.RequestUri!.ToString()
        );
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, null)]
    [InlineData(HttpStatusCode.OK, "invalid-json")]
    public async Task GetInstance_ThrowsException_ForInvalidResponse(HttpStatusCode statusCode, string? content)
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(statusCode, contentFactory: _ => content);
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var record = await Record.ExceptionAsync(() =>
            fixture.FiksArkivInstanceClient.GetInstance(_defaultInstanceIdentifier)
        );

        // Assert
        Assert.IsType<PlatformHttpException>(record);
        Assert.Equal(statusCode, ((PlatformHttpException)record).Response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("some-action")]
    public async Task ProcessMoveNext_CallsCorrectEndpoint(string? action)
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var expectedPayload = await FiksArkivInstanceClient.GetProcessNextAction(action).ReadAsStringAsync();
        var appMetadata = await fixture.AppMetadata.GetApplicationMetadata();

        List<CapturedHttpRequest<string>> requests = [];
        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(
            HttpStatusCode.OK,
            contentFactory: request => IsTokenRequest(request) ? LocaltestToken : null,
            requestCallback: request =>
                requests.Add(new CapturedHttpRequest<string>(request, GetRequestContent(request.Content).Result))
        );
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await fixture.FiksArkivInstanceClient.ProcessMoveNext(_defaultInstanceIdentifier, action);

        // Assert
        CapturedHttpRequest<string> processNextRequest = requests.Last();

        Assert.True(action is null ? expectedPayload == string.Empty : expectedPayload.Contains(action));
        Assert.True(expectedPayload == processNextRequest.Content);

        Assert.Equal(HttpMethod.Put, processNextRequest.Request.Method);
        Assert.Equal($"Bearer {LocaltestToken}", processNextRequest.Request.Headers.Authorization!.ToString());
        Assert.Equal(
            $"http://local.altinn.cloud/{appMetadata.AppIdentifier}/instances/{_defaultInstanceIdentifier}/process/next",
            processNextRequest.Request.RequestUri!.ToString()
        );
    }

    [Fact]
    public async Task ProcessMoveNext_ThrowsException_ForInvalidResponse()
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(HttpStatusCode.Forbidden);
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var record = await Record.ExceptionAsync(() =>
            fixture.FiksArkivInstanceClient.ProcessMoveNext(_defaultInstanceIdentifier)
        );

        // Assert
        Assert.IsType<PlatformHttpException>(record);
        Assert.Equal(HttpStatusCode.Forbidden, ((PlatformHttpException)record).Response.StatusCode);
    }

    [Fact]
    public async Task MarkInstanceComplete_CallsCorrectEndpoint()
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var appMetadata = await fixture.AppMetadata.GetApplicationMetadata();

        List<HttpRequestMessage> requests = [];
        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(
            HttpStatusCode.OK,
            contentFactory: request => IsTokenRequest(request) ? LocaltestToken : null,
            requestCallback: request => requests.Add(request)
        );
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await fixture.FiksArkivInstanceClient.MarkInstanceComplete(_defaultInstanceIdentifier);

        // Assert
        HttpRequestMessage markCompletedRequest = requests.Last();

        Assert.Equal(HttpMethod.Post, markCompletedRequest.Method);
        Assert.Equal($"Bearer {LocaltestToken}", markCompletedRequest.Headers.Authorization!.ToString());
        Assert.Equal(
            $"http://local.altinn.cloud/{appMetadata.AppIdentifier}/instances/{_defaultInstanceIdentifier}/complete",
            markCompletedRequest.RequestUri!.ToString()
        );
    }

    [Fact]
    public async Task MarkInstanceComplete_ThrowsException_ForInvalidResponse()
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(HttpStatusCode.Forbidden);
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var record = await Record.ExceptionAsync(() =>
            fixture.FiksArkivInstanceClient.MarkInstanceComplete(_defaultInstanceIdentifier)
        );

        // Assert
        Assert.IsType<PlatformHttpException>(record);
        Assert.Equal(HttpStatusCode.Forbidden, ((PlatformHttpException)record).Response.StatusCode);
    }

    [Fact]
    public async Task InsertBinaryData_CallsCorrectEndpoint_WithCorrectPayload()
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var appMetadata = await fixture.AppMetadata.GetApplicationMetadata();
        var data = "test content"u8.ToArray();
        var dataElement = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = "some-data-type",
            ContentType = "text/plain",
            Filename = "filename.txt",
        };

        // List<HttpRequestMessage> requests = [];
        List<CapturedHttpRequest<byte[]>> requests = [];
        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(
            HttpStatusCode.OK,
            contentFactory: request => IsTokenRequest(request) ? LocaltestToken : JsonSerializer.Serialize(dataElement),
            requestCallback: request =>
                requests.Add(
                    new CapturedHttpRequest<byte[]>(
                        request,
                        request.Content?.ReadAsByteArrayAsync().Result,
                        request.Content?.Headers
                    )
                )
        );
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var result = await fixture.FiksArkivInstanceClient.InsertBinaryData(
            _defaultInstanceIdentifier,
            dataElement.DataType,
            dataElement.ContentType,
            dataElement.Filename,
            new MemoryStream(data)
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataElement.Id, result.Id);
        Assert.Equal(dataElement.DataType, result.DataType);
        Assert.Equal(dataElement.ContentType, result.ContentType);
        Assert.Equal(dataElement.Filename, result.Filename);

        var insertBinaryDataRequest = requests.Last();
        Assert.Equal(HttpMethod.Post, insertBinaryDataRequest.Request.Method);
        Assert.Equal(dataElement.ContentType, insertBinaryDataRequest.ContentHeaders!.ContentType?.MediaType);
        Assert.Equal(dataElement.Filename, insertBinaryDataRequest.ContentHeaders.ContentDisposition?.FileName);
        Assert.Equal(data, insertBinaryDataRequest.Content);
        Assert.Equal($"Bearer {LocaltestToken}", insertBinaryDataRequest.Request.Headers.Authorization!.ToString());
        Assert.Equal(
            $"http://local.altinn.cloud/{appMetadata.AppIdentifier}/instances/{_defaultInstanceIdentifier}/data?dataType={dataElement.DataType}",
            insertBinaryDataRequest.Request.RequestUri!.ToString()
        );
    }

    [Theory]
    [InlineData("", "", "", HttpStatusCode.OK, typeof(FiksArkivException))]
    [InlineData(null, null, null, HttpStatusCode.OK, typeof(FiksArkivException))]
    [InlineData("abc", "abc", "abc", HttpStatusCode.OK, typeof(FiksArkivException))]
    [InlineData("abc", "abc/def", "abc", HttpStatusCode.Forbidden, typeof(PlatformHttpException))]
    public async Task InsertBinaryData_ThrowsException_ForInvalidRequestOrResponse(
        string? dataType,
        string? contentType,
        string? filename,
        HttpStatusCode statusCode,
        Type expectedExceptionType
    )
    {
        // Arrange
        await using var fixture = TestFixture.Create(services => services.AddFiksArkiv());

        var httpClient = TestHelpers.GetHttpClientWithMockedHandlerFactory(statusCode);
        fixture.HttpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var record = await Record.ExceptionAsync(() =>
            fixture.FiksArkivInstanceClient.InsertBinaryData(
                _defaultInstanceIdentifier,
                dataType!,
                contentType!,
                filename!,
                Stream.Null
            )
        );

        // Assert
        Assert.IsType(expectedExceptionType, record);
        if (record is PlatformHttpException ex)
            Assert.Equal(statusCode, ex.Response.StatusCode);
    }

    private static async Task<string> GetRequestContent(HttpContent? potentialContent)
    {
        return potentialContent is not null ? await potentialContent.ReadAsStringAsync() : string.Empty;
    }

    private static bool IsTokenRequest(HttpRequestMessage request) =>
        request.RequestUri!.AbsolutePath.Equals("/Home/GetTestOrgToken");

    private sealed record CapturedHttpRequest<TContent>(
        HttpRequestMessage Request,
        TContent? Content = default,
        HttpContentHeaders? ContentHeaders = null
    );
}

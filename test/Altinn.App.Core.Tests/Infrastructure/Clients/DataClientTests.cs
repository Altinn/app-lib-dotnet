#nullable enable
using System.Net;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Infrastructure.Clients;

public class DataClientTests
{
    private readonly string apiStorageEndpoint = "https://platform.altinn.no/api/storage/";
    
    [Fact]
    public async Task UpdateBinaryData_put_updated_data_and_Return_DataElement()
    {
        Mock<HttpClient> httpClientMock;
        var resultDataelement = new DataElement()
        {
            Id = "aaaa-bbbbb-cccc-dddd"
        };
        IData dataClient = GetDataClientWithMocks(
            HttpStatusCode.Created,
            resultDataelement,
            out httpClientMock);
        var instanceIdentifier = new InstanceIdentifier("501337/d3f3250d-705c-4683-a215-e05ebcbe6071");
        var dataGuid = new Guid("67a5ef12-6e38-41f8-8b42-f91249ebcec0");
        var restult = await dataClient.UpdateBinaryData(instanceIdentifier, "application/json", "test.json", dataGuid, new MemoryStream());
        restult.Should().BeEquivalentTo(resultDataelement);
        Uri expectedUri = new Uri($"{apiStorageEndpoint}instances/{instanceIdentifier}/data/{dataGuid}", UriKind.RelativeOrAbsolute);
        httpClientMock.Verify(_ => _.SendAsync(
            It.Is<HttpRequestMessage>(m => IsExpectedHttpRequest(m, expectedUri, "test.json", "application/json")), 
            CancellationToken.None));
        httpClientMock.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task UpdateBinaryData_returns_exception_when_put_to_storage_result_in_servererror()
    {
        Mock<HttpClient> httpClientMock;
        var resultDataelement = new DataElement()
        {
            Id = "aaaa-bbbbb-cccc-dddd"
        };
        IData dataClient = GetDataClientWithMocks(
            HttpStatusCode.InternalServerError,
            null,
            out httpClientMock);
        var instanceIdentifier = new InstanceIdentifier("501337/d3f3250d-705c-4683-a215-e05ebcbe6071");
        var dataGuid = new Guid("67a5ef12-6e38-41f8-8b42-f91249ebcec0");
        await Assert.ThrowsAsync<PlatformHttpException>(async () => await dataClient.UpdateBinaryData(instanceIdentifier, "application/json", "test.json", dataGuid, new MemoryStream()));
        Uri expectedUri = new Uri($"{apiStorageEndpoint}instances/{instanceIdentifier}/data/{dataGuid}", UriKind.RelativeOrAbsolute);
        httpClientMock.Verify(_ => _.SendAsync(
            It.Is<HttpRequestMessage>(m => IsExpectedHttpRequest(m, expectedUri, "test.json", "application/json")), 
            CancellationToken.None));
        httpClientMock.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task UpdateBinaryData_returns_exception_when_put_to_storage_result_in_conflict()
    {
        Mock<HttpClient> httpClientMock;
        var resultDataelement = new DataElement()
        {
            Id = "aaaa-bbbbb-cccc-dddd"
        };
        IData dataClient = GetDataClientWithMocks(
            HttpStatusCode.Conflict,
            null,
            out httpClientMock);
        var instanceIdentifier = new InstanceIdentifier("501337/d3f3250d-705c-4683-a215-e05ebcbe6071");
        var dataGuid = new Guid("67a5ef12-6e38-41f8-8b42-f91249ebcec0");
        await Assert.ThrowsAsync<PlatformHttpException>(async () => await dataClient.UpdateBinaryData(instanceIdentifier, "application/json", "test.json", dataGuid, new MemoryStream()));
        Uri expectedUri = new Uri($"{apiStorageEndpoint}instances/{instanceIdentifier}/data/{dataGuid}", UriKind.RelativeOrAbsolute);
        httpClientMock.Verify(_ => _.SendAsync(
            It.Is<HttpRequestMessage>(m => IsExpectedHttpRequest(m, expectedUri, "test.json", "application/json")), 
            CancellationToken.None));
        httpClientMock.VerifyNoOtherCalls();
    }

    private IData GetDataClientWithMocks(HttpStatusCode resultStatusCode, DataElement? resultDataelement, out Mock<HttpClient> httpClientMock)
    {
        IOptions<PlatformSettings> platformSettings = Options.Create(new PlatformSettings()
        {
            ApiStorageEndpoint = apiStorageEndpoint
        });
        IOptionsMonitor<AppSettings> appSettings = new TestAppSettingsMonitor(new AppSettings()
        {
            RuntimeCookieName = "AltinnTestCookie"
        });
        Mock<IHttpContextAccessor> httpCtxAccessor = new Mock<IHttpContextAccessor>();
        HttpContext httpCtx = new DefaultHttpContext();
        httpCtx.Request.Headers["Authorization"] = "Bearer dummytesttoken";
        httpCtxAccessor.Setup(_ => _.HttpContext).Returns(httpCtx);
        httpClientMock = new Mock<HttpClient>();
        HttpResponseMessage responseMessage = new HttpResponseMessage();
        responseMessage.StatusCode = resultStatusCode;
        if (resultDataelement != null)
        {
            responseMessage.Content = GetStreamContent(resultDataelement);
        }

        httpClientMock.Setup(_ => _.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None)).ReturnsAsync(responseMessage);
        IData dataClient = new DataClient(platformSettings, NullLogger<DataClient>.Instance, httpCtxAccessor.Object, appSettings, httpClientMock.Object);

        return dataClient;
    }

    private StreamContent GetStreamContent(DataElement dataElement)
    {
        string de = JsonSerializer.Serialize(dataElement);
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(de);
        writer.Flush();
        stream.Position = 0;
        return new StreamContent(stream);
    }

    private bool IsExpectedHttpRequest(HttpRequestMessage actual, Uri expectedUri, string expectedFilename, string expectedContentType)
    {
        IEnumerable<string>? actualContentType;
        IEnumerable<string>? actualContentDisposition;
        var contentTypeSet = actual.Content.Headers.TryGetValues("Content-Type", out actualContentType);
        var contentDispositionSet = actual.Content.Headers.TryGetValues("Content-Disposition", out actualContentDisposition);
        return Uri.Compare(actual.RequestUri, expectedUri, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0
               && contentTypeSet && actualContentType.FirstOrDefault().Equals(expectedContentType)
               && contentDispositionSet && ContentDispositionHeaderValue.Parse(actualContentDisposition.FirstOrDefault()).FileName.Equals(expectedFilename);
    }
}

internal class TestAppSettingsMonitor : IOptionsMonitor<AppSettings>
{
    public TestAppSettingsMonitor(AppSettings appSettings)
    {
        CurrentValue = appSettings;
    }

    public AppSettings Get(string name)
    {
        throw new NotImplementedException();
    }

    public IDisposable OnChange(Action<AppSettings, string> listener)
    {
        throw new NotImplementedException();
    }

    public AppSettings CurrentValue { get; }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using VerifyTests.Http;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.Infrastructure.Clients.Storage;

public class DataClientTestsDataTypes
{
    private const string Org = "org";
    private const string App = "app";
    private const int InstanceOwnerPartyId = 123456;
    private static readonly Guid _instanceId = Guid.Parse("a467c267-2122-41a4-b78a-ae6f94aa7ff7");
    private static readonly Guid _dataGuid = Guid.Parse("b567c367-3122-41a4-b78a-ae6f94aa7ff8");
    private const string ApiStorageEndpoint = "https://storage.localhost/";

    private readonly ApplicationMetadata _appMetadata = new($"{Org}/{App}")
    {
        DataTypes = new List<DataType>
        {
            new()
            {
                Id = "jsonDataType",
                AppLogic = new() { ClassRef = typeof(TestData).FullName },
                AllowedContentTypes = new List<string> { "application/json" },
            },
            new()
            {
                Id = "xmlDataType",
                AppLogic = new() { ClassRef = typeof(TestData).FullName },
                AllowedContentTypes = new List<string> { "application/xml" },
            },
            new()
            {
                Id = "xmlDefaultDataType",
                AppLogic = new() { ClassRef = typeof(TestData).FullName },
                AllowedContentTypes = new List<string> { "application/xml", "application/json" },
            },
            new()
            {
                Id = "jsonDefaultDataType",
                AppLogic = new() { ClassRef = typeof(TestData).FullName },
                AllowedContentTypes = new List<string> { "application/json", "application/xml" },
            },
        },
    };
    private readonly Mock<IAuthenticationTokenResolver> _tokenResolverMock = new(MockBehavior.Strict);
    private readonly Mock<IAppModel> _appModelMock = new(MockBehavior.Strict);
    private readonly Mock<IAppMetadata> _appMetadataMock = new(MockBehavior.Strict);
    private readonly MockHttpClient _mockHttpClient;
    private readonly IServiceCollection _services = new ServiceCollection();

    private record FakeResponse(
        HttpMethod Method,
        string Url,
        string? RequestContentType,
        HttpStatusCode Status,
        string ResponseType,
        string Content
    );

    private readonly List<FakeResponse> _fakeResponses = new();

    private HttpResponseMessage ResponseBuilder(HttpRequestMessage request)
    {
        var fakeResponse = _fakeResponses.FirstOrDefault(r =>
            r.Url == request.RequestUri?.AbsoluteUri
            && r.Method == request.Method
            && r.RequestContentType == request.Content?.Headers.ContentType?.MediaType
        );
        if (fakeResponse is null)
        {
            throw new Exception(
                $"No response found for request uri: \n{request.Method} {request.RequestUri} {request.Content?.Headers.ContentType}\nAvailable:\n{string.Join("\n", _fakeResponses.Select(r => $"{r.Method} {r.Url} {r.RequestContentType}"))}"
            );
        }
        return new HttpResponseMessage(fakeResponse.Status)
        {
            Content = new StringContent(fakeResponse.Content, Encoding.UTF8, fakeResponse.ResponseType),
        };
    }

    public DataClientTestsDataTypes(ITestOutputHelper outputHelper)
    {
        _appMetadataMock.Setup(a => a.GetApplicationMetadata()).ReturnsAsync(_appMetadata);
        _tokenResolverMock
            .Setup(tr => tr.GetAccessToken(It.IsAny<AuthenticationMethod>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JwtToken.Parse(TestAuthentication.GetUserAuthentication().Token))
            .Verifiable(Times.AtLeastOnce);
        _appModelMock.Setup(a => a.GetModelType(typeof(TestData).FullName!)).Returns(typeof(TestData));
        _mockHttpClient = new MockHttpClient(ResponseBuilder);
        _services.AddSingleton<HttpClient>(_mockHttpClient);
        _services.AddSingleton(_appModelMock.Object);
        _services.AddSingleton<IDataClient, DataClient>();
        _services.AddSingleton(_tokenResolverMock.Object);
        _services.AddSingleton(_appMetadataMock.Object);
        _services.AddSingleton(Options.Create(new PlatformSettings() { ApiStorageEndpoint = ApiStorageEndpoint }));
        _services.AddSingleton<ModelSerializationService>();

        _services.AddFakeLoggingWithXunit(outputHelper);
    }

    private async Task VerifyMocks(object data, string dataType)
    {
        _appMetadataMock.Verify();
        _appModelMock.Verify();
        _tokenResolverMock.Verify();
        await Verify(new { data, _mockHttpClient }).UseParameters(dataType).DontScrubGuids();
    }

    public static TheoryData<string, string> DataTypes =>
        new()
        {
            { "jsonDataType", "application/json" },
            { "xmlDataType", "application/xml" },
            { "xmlDefaultDataType", "application/xml" },
            { "jsonDefaultDataType", "application/json" },
        };

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestObsoleteInsertFormData(string dataType, string requestContentType)
    {
        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Post,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data?dataType={dataType}",
                requestContentType,
                HttpStatusCode.OK,
                "application/json",
                JsonSerializer.Serialize(
                    new DataElement
                    {
                        Id = _dataGuid.ToString(),
                        InstanceGuid = _instanceId.ToString(),
                        ContentType = requestContentType,
                        DataType = dataType,
                    }
                )
            )
        );
        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var element = await dataClient.InsertFormData(
            new TestData { Name = "ivar", Age = 36 },
            _instanceId,
            typeof(TestData),
            Org,
            App,
            InstanceOwnerPartyId,
            dataType
        );

        await VerifyMocks(element, dataType);
    }

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestObsoleteInsertFormDataWithInstance(string dataType, string requestContentType)
    {
        var instance = new Instance
        {
            Id = $"{InstanceOwnerPartyId}/{_instanceId}",
            InstanceOwner = new InstanceOwner { PartyId = InstanceOwnerPartyId.ToString() },
        };
        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Post,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data?dataType={dataType}",
                requestContentType,
                HttpStatusCode.OK,
                "application/json",
                JsonSerializer.Serialize(
                    new DataElement
                    {
                        Id = _dataGuid.ToString(),
                        InstanceGuid = _instanceId.ToString(),
                        ContentType = requestContentType,
                        DataType = dataType,
                    }
                )
            )
        );
        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var element = await dataClient.InsertFormData(
            instance,
            dataType,
            new TestData { Name = "ivar", Age = 36 },
            typeof(TestData)
        );

        await VerifyMocks(element, dataType);
    }

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestInsertFormDataWithInstance(string dataType, string requestContentType)
    {
        var instance = new Instance
        {
            Id = $"{InstanceOwnerPartyId}/{_instanceId}",
            InstanceOwner = new InstanceOwner { PartyId = InstanceOwnerPartyId.ToString() },
        };
        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Post,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data?dataType={dataType}",
                requestContentType,
                HttpStatusCode.OK,
                "application/json",
                JsonSerializer.Serialize(
                    new DataElement
                    {
                        Id = _dataGuid.ToString(),
                        InstanceGuid = _instanceId.ToString(),
                        ContentType = requestContentType,
                        DataType = dataType,
                    }
                )
            )
        );
        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var element = await dataClient.InsertFormData(instance, dataType, new TestData { Name = "ivar", Age = 36 });

        await VerifyMocks(element, dataType);
    }

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestObsoleteUpdateData(string dataType, string requestContentType)
    {
        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Put,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                requestContentType,
                HttpStatusCode.OK,
                "application/json",
                JsonSerializer.Serialize(
                    new DataElement
                    {
                        Id = _dataGuid.ToString(),
                        InstanceGuid = _instanceId.ToString(),
                        ContentType = requestContentType,
                        DataType = dataType,
                    }
                )
            )
        );
        // The tests share the same ClassRef, and the compatibility check will fail if any type supports json
        _appMetadata.DataTypes.RemoveAll(d => d.Id != dataType);

        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var act = async () =>
            await dataClient.UpdateData(
                new TestData { Name = "ivar", Age = 36 },
                _instanceId,
                typeof(TestData),
                Org,
                App,
                InstanceOwnerPartyId,
                _dataGuid
            );

        if (dataType == "xmlDataType")
        {
            var element = await act();
            await VerifyMocks(element, dataType);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }
    }

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestUpdateFormData(string dataType, string requestContentType)
    {
        var dataElement = new DataElement
        {
            Id = _dataGuid.ToString(),
            InstanceGuid = _instanceId.ToString(),
            ContentType = requestContentType,
            DataType = dataType,
        };
        var instance = new Instance
        {
            Id = $"{InstanceOwnerPartyId}/{_instanceId}",
            InstanceOwner = new InstanceOwner { PartyId = InstanceOwnerPartyId.ToString() },
            Data = [dataElement],
        };

        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Put,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                requestContentType,
                HttpStatusCode.OK,
                "application/json",
                JsonSerializer.Serialize(dataElement)
            )
        );
        // The tests share the same ClassRef, and the compatibility check will fail if any type supports json
        _appMetadata.DataTypes.RemoveAll(d => d.Id != dataType);

        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var element = await dataClient.UpdateFormData(instance, new TestData { Name = "ivar", Age = 36 }, dataElement);

        await VerifyMocks(element, dataType);
    }

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestObsoleteGetFormData(string dataType, string storedContentType)
    {
        var storedContent = storedContentType switch
        {
            "application/json" => """{"Name":"ivar","Age":36}""",
            "application/xml" => """
                <?xml version="1.0" encoding="utf-8" standalone="no"?>
                <TestData xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                    <Name>ivar</Name>
                    <Age>36</Age>
                </TestData>
                """,
            _ => throw new NotSupportedException($"Content type {storedContentType} not supported"),
        };
        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Get,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                null,
                HttpStatusCode.OK,
                storedContentType,
                storedContent
            )
        );
        // The tests share the same ClassRef, and the compatibility check will fail if any type supports json
        _appMetadata.DataTypes.RemoveAll(d => d.Id != dataType);

        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var act = async () =>
            await dataClient.GetFormData(_instanceId, typeof(TestData), Org, App, InstanceOwnerPartyId, _dataGuid);
        if (dataType == "xmlDataType")
        {
            var data = await act();
            var typedData = Assert.IsType<TestData>(data);
            Assert.Equal("ivar", typedData.Name);
            Assert.Equal(36, typedData.Age);
            await VerifyMocks(data, dataType);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }
    }

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestGetFormData(string dataType, string storedContentType)
    {
        var dataElement = new DataElement()
        {
            Id = _dataGuid.ToString(),
            InstanceGuid = _instanceId.ToString(),
            ContentType = storedContentType,
            DataType = dataType,
        };
        var instance = new Instance()
        {
            Id = $"{InstanceOwnerPartyId}/{_instanceId}",
            InstanceOwner = new InstanceOwner { PartyId = InstanceOwnerPartyId.ToString() },
            Data = [dataElement],
        };
        var storedContent = storedContentType switch
        {
            "application/json" => """{"Name":"ivar","Age":36}""",
            "application/xml" => """
                <?xml version="1.0" encoding="utf-8" standalone="no"?>
                <TestData xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                    <Name>ivar</Name>
                    <Age>36</Age>
                </TestData>
                """,
            _ => throw new NotSupportedException($"Content type {storedContentType} not supported"),
        };
        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Get,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                null,
                HttpStatusCode.OK,
                storedContentType,
                storedContent
            )
        );
        // The tests share the same ClassRef, and the compatibility check will fail if any type supports json
        _appMetadata.DataTypes.RemoveAll(d => d.Id != dataType);

        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var data = await dataClient.GetFormData(instance, dataElement);
        var typedData = Assert.IsType<TestData>(data);
        Assert.Equal("ivar", typedData.Name);
        Assert.Equal(36, typedData.Age);
        await VerifyMocks(data, dataType);
    }

    [Theory]
    [MemberData(nameof(DataTypes))]
    public async Task TestGetFormDataExtension(string dataType, string storedContentType)
    {
        var dataElement = new DataElement()
        {
            Id = _dataGuid.ToString(),
            InstanceGuid = _instanceId.ToString(),
            ContentType = storedContentType,
            DataType = dataType,
        };
        var instance = new Instance()
        {
            Id = $"{InstanceOwnerPartyId}/{_instanceId}",
            InstanceOwner = new InstanceOwner { PartyId = InstanceOwnerPartyId.ToString() },
            Data = [dataElement],
        };
        var storedContent = storedContentType switch
        {
            "application/json" => """{"Name":"ivar","Age":36}""",
            "application/xml" => """
                <?xml version="1.0" encoding="utf-8" standalone="no"?>
                <TestData xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                    <Name>ivar</Name>
                    <Age>36</Age>
                </TestData>
                """,
            _ => throw new NotSupportedException($"Content type {storedContentType} not supported"),
        };
        _fakeResponses.Add(
            new FakeResponse(
                HttpMethod.Get,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                null,
                HttpStatusCode.OK,
                storedContentType,
                storedContent
            )
        );
        // The tests share the same ClassRef, and the compatibility check will fail if any type supports json
        _appMetadata.DataTypes.RemoveAll(d => d.Id != dataType);

        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        TestData typedData = await dataClient.GetFormData<TestData>(instance, dataElement);
        Assert.Equal("ivar", typedData.Name);
        Assert.Equal(36, typedData.Age);

        // Get code coverage on invalid cast
        await Assert.ThrowsAsync<InvalidCastException>(async () =>
            await dataClient.GetFormData<DataClientTestsDataTypes>(instance, dataElement)
        );
        await VerifyMocks(typedData, dataType);
    }

    [Fact]
    public async Task TestGetBinaryData_AllVariants()
    {
        var content = "binary-content";
        _fakeResponses.Add(
            new(
                HttpMethod.Get,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                null,
                HttpStatusCode.OK,
                "application/octet-stream",
                content
            )
        );
        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();

        // Test Obsolete byte[] method signature
        var obsoleteByteArray = await dataClient.GetDataBytes(Org, App, InstanceOwnerPartyId, _instanceId, _dataGuid);
        var obsoleteByteArrayContent = Encoding.UTF8.GetString(obsoleteByteArray);
        Assert.Equal(content, obsoleteByteArrayContent);
        // Test current byte[] method signature
        var byteArray = await dataClient.GetDataBytes(InstanceOwnerPartyId, _instanceId, _dataGuid);
        var byteArrayContent = Encoding.UTF8.GetString(byteArray);
        Assert.Equal(content, byteArrayContent);
        // Test obsolete method signature
        using (var stream = await dataClient.GetBinaryData(Org, App, InstanceOwnerPartyId, _instanceId, _dataGuid))
        {
            var bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes, 0, bytes.Length);
            var resultContent = Encoding.UTF8.GetString(bytes);
            Assert.Equal(content, resultContent);
        }
        // Test current method signature
        using (var stream = await dataClient.GetBinaryData(InstanceOwnerPartyId, _instanceId, _dataGuid))
        {
            var bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes, 0, bytes.Length);
            var resultContent = Encoding.UTF8.GetString(bytes);
            Assert.Equal(content, resultContent);
            await VerifyMocks(resultContent, "binary");
        }
    }

    [Fact]
    public async Task TestGetBinaryData_TeaPotResult()
    {
        var content = "binary-content";
        _fakeResponses.Add(
            new(
                HttpMethod.Get,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                null,
                HttpStatusCode.Unused,
                "application/octet-stream",
                content
            )
        );
        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();

        // Test obsolete method signature
        var exception = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            dataClient.GetBinaryData(Org, App, InstanceOwnerPartyId, _instanceId, _dataGuid)
        );
        Assert.Equal(HttpStatusCode.Unused, exception.Response.StatusCode);

        // Test current method signature
        exception = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            dataClient.GetBinaryData(InstanceOwnerPartyId, _instanceId, _dataGuid)
        );
        Assert.Equal(HttpStatusCode.Unused, exception.Response.StatusCode);

        // Test Obsolete byte[] method signature
        exception = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            dataClient.GetDataBytes(Org, App, InstanceOwnerPartyId, _instanceId, _dataGuid)
        );
        Assert.Equal(HttpStatusCode.Unused, exception.Response.StatusCode);

        // Test current byte[] method signature
        exception = await Assert.ThrowsAsync<PlatformHttpException>(() =>
            dataClient.GetDataBytes(InstanceOwnerPartyId, _instanceId, _dataGuid)
        );
        Assert.Equal(HttpStatusCode.Unused, exception.Response.StatusCode);

        await VerifyMocks(content, "binary-teapot");
    }

    [Fact]
    public async Task UpdateBinaryData()
    {
        var instanceIdentifier = new InstanceIdentifier(InstanceOwnerPartyId, _instanceId);

        var dataElement = new DataElement
        {
            Id = _dataGuid.ToString(),
            InstanceGuid = _instanceId.ToString(),
            ContentType = "application/pdf",
            DataType = "binaryDataType",
        };
        _fakeResponses.Add(
            new(
                HttpMethod.Put,
                $"{ApiStorageEndpoint}instances/{InstanceOwnerPartyId}/{_instanceId}/data/{_dataGuid}",
                "application/pdf",
                HttpStatusCode.OK,
                "application/json",
                JsonSerializer.Serialize(dataElement)
            )
        );

        await using var serviceProvider = _services.BuildServiceProvider();
        var dataClient = serviceProvider.GetRequiredService<IDataClient>();
        var result = await dataClient.UpdateBinaryData(
            instanceIdentifier,
            "application/pdf",
            "filename",
            _dataGuid,
            new MemoryStream(Encoding.UTF8.GetBytes("binary-content"))
        );

        await VerifyMocks(result, "binary-teapot");
    }

    public class TestData
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
    };
}

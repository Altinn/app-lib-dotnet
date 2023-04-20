#nullable enable
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Models;
using Altinn.App.PlatformServices.Tests.Data;
using Altinn.App.PlatformServices.Tests.Mocks;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Infrastructure.Clients
{
    public class DataClientTests
    {
        private readonly Mock<IOptions<PlatformSettings>> platformSettingsOptions;
        private readonly Mock<IUserTokenProvider> userTokenProvide;
        private readonly ILogger<DataClient> logger;
        private readonly string apiStorageEndpoint = "https://local.platform.altinn.no/api/storage/";

        public DataClientTests()
        {
            platformSettingsOptions = new Mock<IOptions<PlatformSettings>>();
            PlatformSettings platformSettings = new() { ApiStorageEndpoint = apiStorageEndpoint };
            platformSettingsOptions.Setup(s => s.Value).Returns(platformSettings);
            userTokenProvide = new Mock<IUserTokenProvider>();
            userTokenProvide.Setup(u => u.GetUserToken()).Returns("dummytesttoken");

            logger = new NullLogger<DataClient>();
        }

        [Fact]
        public async Task InsertBinaryData_MethodProduceValidPlatformRequest()
        {
            // Arrange
            HttpRequestMessage platformRequest = null;

            var target = GetDataClient(async (HttpRequestMessage request, CancellationToken token) =>
            {
                platformRequest = request;

                DataElement dataElement = new DataElement
                {
                    Id = "DataElement.Id",
                    InstanceGuid = "InstanceGuid"
                };
                await Task.CompletedTask;
                return new HttpResponseMessage() { Content = JsonContent.Create(dataElement) };
            });

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("This is not a pdf, but no one here will care."));
            var instanceIdentifier = new InstanceIdentifier(323413, Guid.NewGuid());
            Uri expectedUri = new Uri($"{apiStorageEndpoint}instances/{instanceIdentifier}/data?dataType=catstories", UriKind.RelativeOrAbsolute);

            // Act
            DataElement actual = await target.InsertBinaryData(instanceIdentifier.ToString(), "catstories", "application/pdf", "a cats story.pdf", stream);

            // Assert
            Assert.NotNull(actual);
            
            Assert.NotNull(platformRequest);
            AssertHttpRequest(platformRequest, expectedUri, HttpMethod.Post, "\"a cats story.pdf\"", "application/pdf");
        }

        [Fact]
        public async Task GetFormData_MethodProduceValidPlatformRequest_ReturnedFormIsValid()
        {
            // Arrange
            HttpRequestMessage platformRequest = null;

            var target = GetDataClient(async (HttpRequestMessage request, CancellationToken token) =>
            {
                platformRequest = request;

                string serializedModel = string.Empty
                                         + @"<?xml version=""1.0""?>"
                                         + @"<Skjema xmlns=""urn:no:altinn:skjema:v1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"""
                                         + @"  skjemanummer=""1472"" spesifikasjonsnummer=""9812"" blankettnummer=""AFP-01"" tittel=""Arbeidsgiverskjema AFP"" gruppeid=""8818"">"
                                         + @"  <Foretak-grp-8820 gruppeid=""8820"">"
                                         + @"    <EnhetNavnEndring-datadef-31 orid=""31"">Test Test 123</EnhetNavnEndring-datadef-31>"
                                         + @"  </Foretak-grp-8820>"
                                         + @"</Skjema>";
                await Task.CompletedTask;

                HttpResponseMessage response = new HttpResponseMessage() { Content = new StringContent(serializedModel) };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                return response;
            });

            Guid dataElementGuid = Guid.NewGuid();
            var instanceIdentifier = new InstanceIdentifier(323413, Guid.NewGuid());
            Uri expectedUri = new Uri($"{apiStorageEndpoint}instances/{instanceIdentifier}/data/{dataElementGuid}", UriKind.RelativeOrAbsolute);

            // Act
            object response = await target.GetFormData(instanceIdentifier.InstanceGuid, typeof(SkjemaWithNamespace), "org", "app", 323413, dataElementGuid);

            // Assert
            var actual = response as SkjemaWithNamespace; 
            Assert.NotNull(actual);
            Assert.NotNull(actual.Foretakgrp8820);
            Assert.NotNull(actual.Foretakgrp8820.EnhetNavnEndringdatadef31);

            Assert.NotNull(platformRequest);
            AssertHttpRequest(platformRequest, expectedUri, HttpMethod.Get, null, "application/xml");
        }

        [Fact]
        public async Task InsertBinaryData_PlatformRespondNotOk_ThrowsPlatformException()
        {
            // Arrange
            HttpRequestMessage platformRequest = null;
            var target = GetDataClient(async (HttpRequestMessage request, CancellationToken token) =>
            {
                platformRequest = request;

                await Task.CompletedTask;
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest };
            });

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("This is not a pdf, but no one here will care."));

            // Act
            var actual = await Assert.ThrowsAsync<PlatformHttpException>(async () => await target.InsertBinaryData("instanceId", "catstories", "application/pdf", "a cats story.pdf", stream));

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(HttpStatusCode.BadRequest, actual.Response.StatusCode);
        }
        
        [Fact]
        public async Task UpdateBinaryData_put_updated_data_and_Return_DataElement()
        {
            var instanceIdentifier = new InstanceIdentifier("501337/d3f3250d-705c-4683-a215-e05ebcbe6071");
            var dataGuid = new Guid("67a5ef12-6e38-41f8-8b42-f91249ebcec0");
            HttpRequestMessage platformRequest = null;
            int invokations = 0;
            DataElement expectedDataelement = new DataElement
            {
                Id = instanceIdentifier.ToString(),
                InstanceGuid = instanceIdentifier.InstanceGuid.ToString()
            };
            var dataClient = GetDataClient(async (HttpRequestMessage request, CancellationToken token) =>
            {
                invokations++;
                platformRequest = request;
                
                DataElement dataElement = new DataElement
                {
                    Id = instanceIdentifier.ToString(),
                    InstanceGuid = instanceIdentifier.InstanceGuid.ToString()
                };
                await Task.CompletedTask;
                return new HttpResponseMessage() { Content = JsonContent.Create(dataElement) };
            });
            Uri expectedUri = new Uri($"{apiStorageEndpoint}instances/{instanceIdentifier}/data/{dataGuid}", UriKind.RelativeOrAbsolute);
            var restult = await dataClient.UpdateBinaryData(instanceIdentifier, "application/json", "test.json", dataGuid, new MemoryStream());
            invokations.Should().Be(1);
            AssertHttpRequest(platformRequest, expectedUri, HttpMethod.Put, "test.json", "application/json");
            restult.Should().BeEquivalentTo(expectedDataelement);
        }

        [Fact]
        public async Task UpdateBinaryData_returns_exception_when_put_to_storage_result_in_servererror()
        {
            var instanceIdentifier = new InstanceIdentifier("501337/d3f3250d-705c-4683-a215-e05ebcbe6071");
            var dataGuid = new Guid("67a5ef12-6e38-41f8-8b42-f91249ebcec0");
            int invokations = 0;
            var dataClient = GetDataClient(async (HttpRequestMessage request, CancellationToken token) =>
            {
                invokations++;
                
                await Task.CompletedTask;
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.InternalServerError };
            });
            var actual = await Assert.ThrowsAsync<PlatformHttpException>(async () => await dataClient.UpdateBinaryData(instanceIdentifier, "application/json", "test.json", dataGuid, new MemoryStream()));
            actual.Should().NotBeNull();
            actual.Response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
        
        [Fact]
        public async Task UpdateBinaryData_returns_exception_when_put_to_storage_result_in_conflict()
        {
            var instanceIdentifier = new InstanceIdentifier("501337/d3f3250d-705c-4683-a215-e05ebcbe6071");
            var dataGuid = new Guid("67a5ef12-6e38-41f8-8b42-f91249ebcec0");
            int invokations = 0;
            var dataClient = GetDataClient(async (HttpRequestMessage request, CancellationToken token) =>
            {
                invokations++;
                
                await Task.CompletedTask;
                return new HttpResponseMessage() { StatusCode = HttpStatusCode.Conflict };
            });
            var actual = await Assert.ThrowsAsync<PlatformHttpException>(async () => await dataClient.UpdateBinaryData(instanceIdentifier, "application/json", "test.json", dataGuid, new MemoryStream()));
            actual.Should().NotBeNull();
            actual.Response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        private DataClient GetDataClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
        {
            DelegatingHandlerStub dhStub = new(handlerFunc);
            return new DataClient(
                platformSettingsOptions.Object,
                logger,
                new HttpClient(dhStub),
                userTokenProvide.Object);
        }

        private void AssertHttpRequest(HttpRequestMessage actual, Uri expectedUri, HttpMethod method, string? expectedFilename, string? expectedContentType)
        {
            IEnumerable<string>? actualContentType = null;
            IEnumerable<string>? actualContentDisposition = null;
            actual.Content?.Headers.TryGetValues("Content-Type", out actualContentType);
            actual.Content?.Headers.TryGetValues("Content-Disposition", out actualContentDisposition);
            var authHeader = actual.Headers.Authorization;
            actual.RequestUri.Should().BeEquivalentTo(expectedUri);
            Uri.Compare(actual.RequestUri, expectedUri, UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase).Should().Be(0, "Actual request Uri did not match expected Uri");
            if (expectedContentType is not null)
            {
                actualContentType?.FirstOrDefault().Should().BeEquivalentTo(expectedContentType);
            }
            
            if (expectedFilename is not null)
            {
                ContentDispositionHeaderValue.Parse(actualContentDisposition?.FirstOrDefault()).FileName?.Should().BeEquivalentTo(expectedFilename);
            }

            authHeader.Parameter.Should().BeEquivalentTo("dummytesttoken");
        }
    }
}

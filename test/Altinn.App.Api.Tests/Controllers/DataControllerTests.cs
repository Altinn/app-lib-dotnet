﻿using Altinn.App.Api.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net;
using Xunit;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.Features.FileAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Altinn.App.Core.Features.Validation;

namespace Altinn.App.Api.Tests.Controllers
{
    public class DataControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public DataControllerTests(WebApplicationFactory<Program> factory) : base(factory)
        {
            OverrideServicesForAllTests = (services) =>
            {
                services.AddTransient<IFileAnalyser, MimeTypeAnalyser>();
                services.AddTransient<IFileValidator, MimeTypeValidator>();
            };
        }

        [Fact]
        public async Task CreateDataElement_BinaryPdf_AnalyserShouldRunOk()
        {
            // Setup test data
            string org = "tdd";
            string app = "contributer-restriction";
            HttpClient client = GetRootedClient(org, app);
 
            Guid guid = new Guid("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd");
            TestData.DeleteInstance(org, app, 1337, guid);
            TestData.PrepareInstance(org, app, 1337, guid);

            // Setup the request
            string token = PrincipalUtil.GetOrgToken("nav", "160694123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ByteArrayContent fileContent = await CreateBinaryContent(org, app, "example.pdf", "application/pdf");
            string url = $"/{org}/{app}/instances/1337/{guid}/data?dataType=specificFileType";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = fileContent
            };

            // This is where it happens
            HttpResponseMessage response = await client.SendAsync(request);
            request.Dispose();

            // Cleanup testdata
            TestData.DeleteInstanceAndData(org, app, 1337, guid);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateDataElement_JpgFakedAsPdf_AnalyserShouldRunAndFail()
        {
            // Setup test data
            string org = "tdd";
            string app = "contributer-restriction";
            HttpClient client = GetRootedClient(org, app);

            Guid guid = new Guid("1fc98a23-fe31-4ef5-8fb9-dd3f479354ce");
            TestData.DeleteInstance(org, app, 1337, guid);
            TestData.PrepareInstance(org, app, 1337, guid);

            // Setup the request
            string token = PrincipalUtil.GetOrgToken("nav", "160694123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ByteArrayContent fileContent = await CreateBinaryContent(org, app, "example.jpg.pdf", "application/pdf");
            string url = $"/{org}/{app}/instances/1337/{guid}/data?dataType=specificFileType";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = fileContent
            };

            // This is where it happens
            HttpResponseMessage response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            request.Dispose();

            // Cleanup testdata
            TestData.DeleteInstanceAndData(org, app, 1337, guid);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private static async Task<ByteArrayContent> CreateBinaryContent(string org, string app, string filename, string mediaType)
        {
            var pdfFilePath = TestData.GetAppSpecificTestdataFile(org, app, filename);
            var fileBytes = await File.ReadAllBytesAsync(pdfFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            fileContent.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse($"attachment; filename=\"{filename}\"; filename*=UTF-8''{filename}");
            return fileContent;
        }
    }
}

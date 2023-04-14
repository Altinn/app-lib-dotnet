using Altinn.App.Api.Tests.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net;
using Xunit;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.Features.FileAnalyzis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Altinn.App.Api.Tests.Controllers
{
    public class DataControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public DataControllerTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateDataElement_BinaryPdf_AnalyzerShouldRun()
        {
            OverrideServicesForThisTest = (services) =>
            {
                services.AddTransient<IFileAnalyzer, MimeTypeAnalyzer>();
            };

            // Setup test data
            string org = "tdd";
            string app = "contributer-restriction";
            HttpClient client = GetRootedClient(org, app);

            Guid guid = new Guid("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd");
            TestDataUtil.DeleteInstance(org, app, 1337, guid);
            TestDataUtil.PrepareInstance(org, app, 1337, guid);

            string token = PrincipalUtil.GetOrgToken("nav", "160694123");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Build the binary content
            var pdfFilePath = TestData.GetAppSpecificTestdataFile(org, app, "example.pdf");
            var fileBytes = await File.ReadAllBytesAsync(pdfFilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            fileContent.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("attachment; filename=\"example.pdf\"; filename*=UTF-8''example.pdf");

            // This is where it happens
            string url = $"/{org}/{app}/instances/1337/{guid}/data?dataType=specificFileType";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = fileContent;
            HttpResponseMessage response = await client.SendAsync(request);            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Cleanup testdata
            TestDataUtil.DeleteInstanceAndData(org, app, 1337, guid);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}

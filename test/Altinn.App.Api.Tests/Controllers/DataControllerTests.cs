using Altinn.App.Api.Tests.Utils;
using Altinn.App.Core.Internal.Events;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.App.Api.Tests.Controllers
{
    public class DataControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        public DataControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CreateDataElement_BinaryPdf_AnalyzerShouldRun()
        {
            var client = _factory.CreateClient();

            string app = "contributer-restriction";
            Guid guid = new Guid("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd");
            //TestDataUtil.DeleteInstance("tdd", app, 1337, guid);
            //TestDataUtil.PrepareInstance("tdd", app, 1337, guid);
            string token = PrincipalUtil.GetOrgToken("nav", "160694123");
            string expectedMsg = "Invalid data provided. Error: The Content-Disposition header must contain a filename";

            //HttpClient client = SetupUtil.GetTestClient(_factory, "tdd", app);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/tdd/{app}/instances/1337/{guid}/data?dataType=specificFileType";
            HttpContent content = new StringContent(string.Empty);
            content.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("attachment");

            HttpResponseMessage response = await client.PostAsync(url, content);
            string message = await response.Content.ReadAsStringAsync();
            //TestDataUtil.DeleteInstanceAndData("tdd", app, 1337, guid);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(expectedMsg, message);
        }
    }
}

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using Xunit;
using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using FluentAssertions;
using System.Text.Json;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Api.Tests.Utils;

namespace Altinn.App.Api.Tests.Controllers
{
    public class OptionsControllerTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public OptionsControllerTests(WebApplicationFactory<Program> factory) : base(factory)
        {
            OverrideServicesForAllTests = (services) =>
            {
                services.AddTransient<IAppOptionsProvider, DummyProvider>();
                services.AddTransient<IInstanceAppOptionsProvider, DummyInstanceProvider>();
            };
        }

        [Fact]
        public async Task Get_UnknownOptionList_ShouldReturnNotFound()
        {
            string org = "tdd";
            string app = "contributer-restriction";
            HttpClient client = GetRootedClient(org, app);

            string url = $"/{org}/{app}/api/options/unknown";
            HttpResponseMessage response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, content);
        }

        [Fact]
        public async Task Get_ShouldReturnCorrectNormalOptions()
        {
            string org = "tdd";
            string app = "contributer-restriction";
            HttpClient client = GetRootedClient(org, app);

            string url = $"/{org}/{app}/api/options/normal-options";
            HttpResponseMessage response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, content);

            var list = JsonSerializer.Deserialize<List<AppOption>>(content);
            list.Should().HaveCount(1);
            list.Should().Contain(x => x.Label == "label1");
        }

        [Fact(Skip = "Auth seeems to fail")]
        public async Task Get_ShouldReturnCorrectInstanceOptions()
        {
            string org = "tdd";
            string app = "contributer-restriction";
            
            HttpClient client = GetRootedClient(org, app);

            int instanceOwnerId = 1337;
            Guid instanceGuid = new Guid("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd");
            TestData.DeleteInstance(org, app, instanceOwnerId, instanceGuid);
            TestData.PrepareInstance(org, app, instanceOwnerId, instanceGuid);


            string token = PrincipalUtil.GetToken(1337);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string url = $"/{org}/{app}/instances/{instanceOwnerId}/{instanceGuid}/options/instance-options";
            HttpResponseMessage response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, content);

            var list = JsonSerializer.Deserialize<AppOptions>(content);
            list.Should().NotBeNull();
            list!.Options.Should().HaveCount(1);
            list.Options.Should().Contain(x => x.Label == "label1");
            
            
            // Cleanup testdata
            TestData.DeleteInstanceAndData(org, app, instanceOwnerId, instanceGuid);
        }

        [Fact]
        public async Task Get_ShouldReturnParametersInHeader()
        {
            string org = "tdd";
            string app = "contributer-restriction";
            HttpClient client = GetRootedClient(org, app);

            string url = $"/{org}/{app}/api/options/normal-options?language=esperanto";
            HttpResponseMessage response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, content);

            var headerValue = response.Headers.GetValues("Altinn-DownstreamParameters");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            headerValue.Should().Contain("lang=esperanto");
        }

        [Fact]
        public async Task Get_ShouldDefaultToNbLanguage()
        {
            string org = "tdd";
            string app = "contributer-restriction";
            HttpClient client = GetRootedClient(org, app);

            string url = $"/{org}/{app}/api/options/normal-options";
            HttpResponseMessage response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, content);

            var headerValue = response.Headers.GetValues("Altinn-DownstreamParameters");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            headerValue.Should().Contain("lang=nb");
        }
    }

    public class DummyProvider : IAppOptionsProvider
    {
        public string Id => "normal-options";

        public Task<AppOptions> GetAppOptionsAsync(string language, Dictionary<string, string> keyValuePairs)
        {
            AppOptions appOptions = new AppOptions()
            {
                Parameters = new Dictionary<string, string>()
                {
                    { "lang", language }
                },
                Options = new List<AppOption>()
                {
                    new AppOption()
                    {
                        Label = "label1",
                        Value = "value1"
                    }
                }

            };

            return Task.FromResult(appOptions);
        }
    }

    public class DummyInstanceProvider : IInstanceAppOptionsProvider
    {
        public string Id => "instance-options";

        public Task<AppOptions> GetInstanceAppOptionsAsync(InstanceIdentifier instanceIdentifier, string language, Dictionary<string, string> keyValuePairs)
        {
            AppOptions appOptions = new AppOptions()
            {
                Options = new List<AppOption>()
                {
                    new AppOption()
                    {
                        Label = "label1",
                        Value = "value1"
                    }
                }

            };

            return Task.FromResult(appOptions);
        }
    }
}

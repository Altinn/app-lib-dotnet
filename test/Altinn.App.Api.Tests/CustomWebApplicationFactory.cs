using Altinn.App.Api.Tests.Data;
using Altinn.App.Core.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Api.Tests
{
    public class ApiTestBase
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ApiTestBase(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Gets a client that adds appsettings from the specified org/app
        /// test application under TestData/Apps to the service collection.
        /// </summary>
        public HttpClient GetRootedClient(string org, string app)
        {
            string appRootPath = TestData.GetApplicationDirectory(org, app);
            string appSettingsPath = Path.Join(appRootPath, "appsettings.json");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                var configuration = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath)
                .Build();

                configuration.GetSection("AppSettings:AppBasePath").Value = appRootPath;
                IConfigurationSection appSettingSection = configuration.GetSection("AppSettings");

                builder.ConfigureServices(services => services.Configure<AppSettings>(appSettingSection));
                builder.ConfigureTestServices(services => OverrideServicesAllTests(services));
                builder.ConfigureTestServices(OverrideServicesForThisTest);
            }).CreateClient();

            return client;
        }

        public Action<IServiceCollection> OverrideServicesAllTests { get; set; }

        public Action<IServiceCollection> OverrideServicesForThisTest { get; set; }
    }
}

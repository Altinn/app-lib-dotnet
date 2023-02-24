using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.Internal.App
{
    public class AppMedataTest
    {
        private readonly string appBasePath = Path.Combine("Internal", "App", "TestData") + Path.DirectorySeparatorChar;

        [Fact]
        public async void GetApplicationMetadata_desrializes_file_from_disk()
        {
            AppSettings appSettings = GetAppSettings("AppMetadata", "default.applicationmetadata.json");
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            ApplicationMetadata expected = new ApplicationMetadata()
            {
                Id = "tdd/bestilling",
                Org = "tdd",
                App = "bestilling",
                Created = DateTime.Parse("2019-09-16T22:22:22"),
                CreatedBy = "username",
                Title = new Dictionary<string, string>()
                {
                    { "nb", "Bestillingseksempelapp" }
                },
                DataTypes = new List<DataType>()
                {
                    new()
                    {
                        Id = "vedlegg",
                        AllowedContentTypes = new List<string>() { "application/pdf", "image/png", "image/jpeg" },
                        MinCount = 0,
                        TaskId = "Task_1"
                    },
                    new()
                    {
                        Id = "ref-data-as-pdf",
                        AllowedContentTypes = new List<string>() { "application/pdf" },
                        MinCount = 1,
                        TaskId = "Task_1"
                    }
                },
                PartyTypesAllowed = new PartyTypesAllowed()
                {
                    BankruptcyEstate = true,
                    Organisation = true,
                    Person = true,
                    SubUnit = true
                },
                OnEntry = new OnEntryConfig()
                {
                    Show = "select-instance"
                },
                Features = new Dictionary<string, bool>()
                {
                    { "footer", true }
                }
            };
            var actual = await appMetadata.GetApplicationMetadata();
            actual.Should().NotBeNull();
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async void GetApplicationMetadata_second_read_from_cache()
        {
            AppSettings appSettings = GetAppSettings("AppMetadata", "default.applicationmetadata.json");
            Mock<IAppFeatures> appFeaturesMock = new Mock<IAppFeatures>();
            appFeaturesMock.Setup(af => af.GetEnabledFeatures()).ReturnsAsync(new Dictionary<string, bool>() { { "footer", true } });
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), appFeaturesMock.Object, new NullLogger<AppMetadata>());
            ApplicationMetadata expected = new ApplicationMetadata()
            {
                Id = "tdd/bestilling",
                Org = "tdd",
                App = "bestilling",
                Created = DateTime.Parse("2019-09-16T22:22:22"),
                CreatedBy = "username",
                Title = new Dictionary<string, string>()
                {
                    { "nb", "Bestillingseksempelapp" }
                },
                DataTypes = new List<DataType>()
                {
                    new()
                    {
                        Id = "vedlegg",
                        AllowedContentTypes = new List<string>() { "application/pdf", "image/png", "image/jpeg" },
                        MinCount = 0,
                        TaskId = "Task_1"
                    },
                    new()
                    {
                        Id = "ref-data-as-pdf",
                        AllowedContentTypes = new List<string>() { "application/pdf" },
                        MinCount = 1,
                        TaskId = "Task_1"
                    }
                },
                PartyTypesAllowed = new PartyTypesAllowed()
                {
                    BankruptcyEstate = true,
                    Organisation = true,
                    Person = true,
                    SubUnit = true
                },
                OnEntry = new OnEntryConfig()
                {
                    Show = "select-instance"
                },
                Features = new Dictionary<string, bool>()
                {
                    { "footer", true }
                }
            };
            var actual = await appMetadata.GetApplicationMetadata();
            var actual2 = await appMetadata.GetApplicationMetadata();
            appFeaturesMock.Verify(af => af.GetEnabledFeatures());
            appFeaturesMock.VerifyAll();
            actual.Should().NotBeNull();
            actual.Should().BeEquivalentTo(expected);
            actual2.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async void GetApplicationMetadata_return_null_if_file_not_found()
        {
            AppSettings appSettings = GetAppSettings("AppMetadata", "notfound.applicationmetadata.json");
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            var actual = await appMetadata.GetApplicationMetadata();
            actual.Should().BeNull();
        }

        [Fact]
        public async void GetApplicationMetadata_return_null_if_deserialization_fails()
        {
            AppSettings appSettings = GetAppSettings("AppMetadata", "invalid.applicationmetadata.json");
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            var actual = await appMetadata.GetApplicationMetadata();
            actual.Should().BeNull();
        }
        
        [Fact]
        public async void GetApplicationXACMLPolicy_return_policyfile_as_string()
        {
            AppSettings appSettings = GetAppSettings(subfolder:"AppPolicy", policyFilename: "policy.xml");
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            string expected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>policy</root>";
            var actual = await appMetadata.GetApplicationXACMLPolicy();
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public async void GetApplicationXACMLPolicy_return_null_if_file_not_found()
        {
            AppSettings appSettings = GetAppSettings(subfolder:"AppPolicy", policyFilename: "notfound.xml");
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            var actual = await appMetadata.GetApplicationXACMLPolicy();
            actual.Should().BeNull();
        }
        
        [Fact]
        public async void GetApplicationBPMNProcess_return_process_as_string()
        {
            AppSettings appSettings = GetAppSettings(subfolder:"AppProcess", bpmnFilename: "process.bpmn");
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            string expected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>process</root>";
            var actual = await appMetadata.GetApplicationBPMNProcess();
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public async void GetApplicationBPMNProcess_return_null_if_file_not_found()
        {
            AppSettings appSettings = GetAppSettings(subfolder:"AppProcess", policyFilename: "notfound.xml");
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            var actual = await appMetadata.GetApplicationBPMNProcess();
            actual.Should().BeNull();
        }

        private AppSettings GetAppSettings(string subfolder, string appMetadataFilename = "", string bpmnFilename = "", string policyFilename = "")
        {
            AppSettings appSettings = new AppSettings()
            {
                AppBasePath = appBasePath,
                ConfigurationFolder = subfolder + Path.DirectorySeparatorChar,
                AuthorizationFolder = string.Empty,
                ProcessFolder = string.Empty,
                ApplicationMetadataFileName = appMetadataFilename,
                ProcessFileName = bpmnFilename,
                ApplicationXACMLPolicyFileName = policyFilename
            };
            return appSettings;
        }
    }
}

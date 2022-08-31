#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Altinn.App.PlatformServices.Configuration;
using Altinn.App.PlatformServices.Implementation;
using Altinn.App.PlatformServices.Interface;
using Altinn.App.PlatformServices.Options;
using Altinn.App.Services.Configuration;
using Altinn.App.Services.Interface;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Implementation
{
    public class PdfServiceTests
    {
        private const string HostName = "digdir.apps.at22.altinn.cloud";

        private readonly Mock<IPDF> _pdf = new();
        private readonly Mock<IAppResources> _appResources = new();
        private readonly Mock<IAppOptionsService> _appOptionsService = new();
        private readonly Mock<IData> _dataClient = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
        private readonly Mock<IPdfGeneratorClient> _pdfGeneratorClient = new();
        private readonly Mock<IProfile> _profile = new();
        private readonly Mock<IRegister> _register = new();
        private readonly Mock<ICustomPdfHandler> _customPdfHandler = new();
        private readonly Mock<IOptions<PdfGeneratorSettings>> _pdfGeneratorSettingsOptions;
        private readonly Mock<IOptions<GeneralSettings>> _generalSettingsOptions;

        public PdfServiceTests()
        {
            DefaultHttpContext httpContext = new();
            httpContext.Request.Protocol = "https";
            httpContext.Request.Host = new(HostName);
            _httpContextAccessor.Setup(s => s.HttpContext!).Returns(httpContext);

            PdfGeneratorSettings pdfGeneratorSettings = new() { ServiceEndpointUri = "http://real.domain.no" };
            _pdfGeneratorSettingsOptions = new Mock<IOptions<PdfGeneratorSettings>>();
            _pdfGeneratorSettingsOptions.Setup(s => s.Value).Returns(pdfGeneratorSettings);

            GeneralSettings generalSettings = new() { HostName = HostName };
            _generalSettingsOptions = new Mock<IOptions<GeneralSettings>>();
            _generalSettingsOptions.Setup(s => s.Value).Returns(generalSettings);
        }

        [Fact]
        public async Task GenerateAndStorePdf()
        {
            // Arrange
            _pdfGeneratorClient.Setup(s => s.GeneratePdf(It.IsAny<Uri>(), It.IsAny<CancellationToken>()));

            var target = new PdfService(
                _pdf.Object, 
                _appResources.Object,
                _appOptionsService.Object,
                _dataClient.Object,
                _httpContextAccessor.Object,
                _profile.Object,
                _register.Object,
                _customPdfHandler.Object,
                _pdfGeneratorClient.Object, 
                _pdfGeneratorSettingsOptions.Object,
                _generalSettingsOptions.Object);
            
            Instance instance = new()
            {
                Id = $"509378/{Guid.NewGuid()}",
                AppId = "digdir/not-really-an-app"
            };
            
            // Act
            await target.GenerateAndStorePdf(instance, CancellationToken.None);

            // Asserts
            _pdfGeneratorClient.Verify(
                s => s.GeneratePdf(
                    It.Is<Uri>(
                        u => u.Scheme == "https" && 
                        u.Host == HostName &&
                        u.AbsoluteUri.Contains(instance.AppId) &&
                        u.AbsoluteUri.Contains(instance.Id)), 
                    It.IsAny<CancellationToken>()), 
                Times.Once);

            _dataClient.Verify(
                s => s.InsertBinaryData(
                    It.Is<string>(s => s == instance.Id),
                    It.Is<string>(s => s == "ref-data-as-pdf"),
                    It.Is<string>(s => s == "application/pdf"),
                    It.Is<string>(s => s == "experimental.pdf"),
                    It.IsAny<Stream>()),
                Times.Once);
        }
    }
}

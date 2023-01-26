#nullable enable

using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.Pdf;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Moq;

using Xunit;

namespace Altinn.App.PlatformServices.Tests.Internal.Pdf
{
    public class PdfServiceTests
    {
        private const string HostName = "at22.altinn.cloud";

        private readonly Mock<IAppResources> _appResources = new();
        private readonly Mock<IData> _dataClient = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
        private readonly Mock<IPdfGeneratorClient> _pdfGeneratorClient = new();
        private readonly Mock<IProfile> _profile = new();
        private readonly Mock<IOptions<PdfGeneratorSettings>> _pdfGeneratorSettingsOptions;
        private readonly Mock<IOptions<GeneralSettings>> _generalSettingsOptions;

        public PdfServiceTests()
        {
            var resource = Task.FromResult(new TextResource() { Id = "digdir-not-really-an-app-nb", Language = "nb", Org = "digdir", Resources = new List<TextResourceElement>() });
            _appResources.Setup(s => s.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(resource);

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
                _appResources.Object,
                _dataClient.Object,
                _httpContextAccessor.Object,
                _profile.Object,
                _pdfGeneratorClient.Object,
                _pdfGeneratorSettingsOptions.Object,
                _generalSettingsOptions.Object);

            Instance instance = new()
            {
                Id = $"509378/{Guid.NewGuid()}",
                AppId = "digdir/not-really-an-app",
                Org = "digdir"
            };

            // Act
            await target.GenerateAndStorePdf(instance, CancellationToken.None);

            // Asserts
            _pdfGeneratorClient.Verify(
                s => s.GeneratePdf(
                    It.Is<Uri>(
                        u => u.Scheme == "https" &&
                        u.Host == $"{instance.Org}.apps.{HostName}" &&
                        u.AbsoluteUri.Contains(instance.AppId) &&
                        u.AbsoluteUri.Contains(instance.Id)),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _dataClient.Verify(
                s => s.InsertBinaryData(
                    It.Is<string>(s => s == instance.Id),
                    It.Is<string>(s => s == "ref-data-as-pdf"),
                    It.Is<string>(s => s == "application/pdf"),
                    It.Is<string>(s => s == "not-really-an-app.pdf"),
                    It.IsAny<Stream>()),
                Times.Once);
        }
    }
}
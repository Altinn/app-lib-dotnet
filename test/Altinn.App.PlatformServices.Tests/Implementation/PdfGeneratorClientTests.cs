#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Altinn.App.PlatformServices.Configuration;
using Altinn.App.PlatformServices.Implementation;
using Altinn.App.PlatformServices.Tests.Mocks;
using Altinn.App.Services;

using Microsoft.Extensions.Options;

using Moq;

using Xunit;

namespace Altinn.App.PlatformServices.Tests.Implementation
{
    public class PdfGeneratorClientTests
    {
        private readonly PdfGeneratorSettings _pdfGeneratorSettings;
        private readonly Mock<IOptions<PdfGeneratorSettings>> _pdfGeneratorSettingsOptions;

        public PdfGeneratorClientTests()
        {
            _pdfGeneratorSettings = new()
            {
                ServiceEndpointUri = "http://real.domain.no"
            };
            _pdfGeneratorSettingsOptions = new Mock<IOptions<PdfGeneratorSettings>>();
            _pdfGeneratorSettingsOptions.Setup(s => s.Value).Returns(_pdfGeneratorSettings);
        }

        [Fact]
        public async Task GeneratePdf_WaitForTime()
        {
            // Arrange
            Mock<IUserTokenProvider> userTokenProvider = new();
            userTokenProvider.Setup(s => s.GetUserToken()).Returns("userToken");

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("this is not a pdf"));
            RequestInterceptor requestInterceptor = new(HttpStatusCode.OK, stream);

            PdfGeneratorClient target = new(
                new HttpClient(requestInterceptor), _pdfGeneratorSettingsOptions.Object, userTokenProvider.Object);

            Uri appUri = new Uri("https://not.an.app.no/yes.an.app.for.reals");

            // Act
            await target.GeneratePdf(appUri, CancellationToken.None);

            // Assert
            string content = await requestInterceptor.GetRequestContentAsStringAsync();

            Assert.Contains("url", content);
            Assert.Contains("printBackground", content);
            Assert.Contains("cookies", content);
            Assert.Contains("userToken", content);
            Assert.Contains("\"waitFor\":5000", content);
        }

        [Fact]
        public async Task GeneratePdf_WaitForSelector()
        {
            // Arrange
            Mock<IUserTokenProvider> userTokenProvider = new();
            userTokenProvider.Setup(s => s.GetUserToken()).Returns("userToken");

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("this is not a pdf"));
            RequestInterceptor requestInterceptor = new(HttpStatusCode.OK, stream);

            _pdfGeneratorSettings.WaitForSelector = "#readyForPrint";

            PdfGeneratorClient target = new(
                new HttpClient(requestInterceptor), _pdfGeneratorSettingsOptions.Object, userTokenProvider.Object);

            Uri appUri = new Uri("https://not.an.app.no/yes.an.app.for.reals");

            // Act
            await target.GeneratePdf(appUri, CancellationToken.None);

            // Assert
            string content = await requestInterceptor.GetRequestContentAsStringAsync();

            Assert.Contains("url", content);
            Assert.Contains("printBackground", content);
            Assert.Contains("cookies", content);
            Assert.Contains("userToken", content);
            Assert.Contains("\"waitFor\":\"#readyForPrint\"", content);
        }
    }
}

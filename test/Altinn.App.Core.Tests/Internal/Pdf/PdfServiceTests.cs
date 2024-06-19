using System.Net;
using System.Security.Claims;
using Altinn.App.Common.Tests;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Infrastructure.Clients.Pdf;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.PlatformServices.Tests.Helpers;
using Altinn.App.PlatformServices.Tests.Mocks;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Storage.Interface.Models;
using AltinnCore.Authentication.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Altinn.App.PlatformServices.Tests.Internal.Pdf;

public class PdfServiceTests
{
    private const string HostName = "at22.altinn.cloud";

    private readonly Mock<IAppResources> _appResources = new();
    private readonly Mock<IDataClient> _dataClient = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly Mock<IPdfGeneratorClient> _pdfGeneratorClient = new();
    private readonly Mock<IProfileClient> _profile = new();
    private readonly IOptions<PdfGeneratorSettings> _pdfGeneratorSettingsOptions = Options.Create<PdfGeneratorSettings>(
        new() { }
    );

    private readonly IOptions<GeneralSettings> _generalSettingsOptions = Options.Create<GeneralSettings>(
        new() { HostName = HostName }
    );

    private readonly IOptions<PlatformSettings> _platformSettingsOptions = Options.Create<PlatformSettings>(new() { });

    private readonly Mock<IUserTokenProvider> _userTokenProvider;

    public PdfServiceTests()
    {
        var resource = new TextResource()
        {
            Id = "digdir-not-really-an-app-nb",
            Language = LanguageConst.Bokmål,
            Org = "digdir",
            Resources = []
        };
        _appResources
            .Setup(s => s.GetTexts(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(resource);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Protocol = "https";
        httpContext.Request.Host = new(HostName);
        _httpContextAccessor.Setup(s => s.HttpContext!).Returns(httpContext);

        _userTokenProvider = new Mock<IUserTokenProvider>();
        _userTokenProvider.Setup(s => s.GetUserToken()).Returns("usertoken");
    }

    [Fact]
    public async Task ValidRequest_ShouldReturnPdf()
    {
        DelegatingHandlerStub delegatingHandler =
            new(
                async (HttpRequestMessage request, CancellationToken token) =>
                {
                    await Task.CompletedTask;
                    return new HttpResponseMessage()
                    {
                        Content = new StreamContent(
                            EmbeddedResource.LoadDataAsStream("Altinn.App.Core.Tests.Internal.Pdf.TestData.example.pdf")
                        )
                    };
                }
            );

        var httpClient = new HttpClient(delegatingHandler);
        var pdfGeneratorClient = new PdfGeneratorClient(
            httpClient,
            _pdfGeneratorSettingsOptions,
            _platformSettingsOptions,
            _userTokenProvider.Object,
            _httpContextAccessor.Object
        );

        Stream pdf = await pdfGeneratorClient.GeneratePdf(
            new Uri(@"https://org.apps.hostName/appId/#/instance/instanceId"),
            CancellationToken.None
        );

        pdf.Length.Should().Be(17814L);
    }

    [Fact]
    public async Task ValidRequest_PdfGenerationFails_ShouldThrowException()
    {
        DelegatingHandlerStub delegatingHandler =
            new(
                async (HttpRequestMessage request, CancellationToken token) =>
                {
                    await Task.CompletedTask;
                    return new HttpResponseMessage() { StatusCode = HttpStatusCode.RequestTimeout };
                }
            );

        var httpClient = new HttpClient(delegatingHandler);
        var pdfGeneratorClient = new PdfGeneratorClient(
            httpClient,
            _pdfGeneratorSettingsOptions,
            _platformSettingsOptions,
            _userTokenProvider.Object,
            _httpContextAccessor.Object
        );

        var func = async () =>
            await pdfGeneratorClient.GeneratePdf(
                new Uri(@"https://org.apps.hostName/appId/#/instance/instanceId"),
                CancellationToken.None
            );

        await func.Should().ThrowAsync<PdfGenerationException>();
    }

    [Fact]
    public async Task GenerateAndStorePdf()
    {
        // Arrange
        TelemetrySink telemetrySink = new();
        _pdfGeneratorClient.Setup(s => s.GeneratePdf(It.IsAny<Uri>(), It.IsAny<CancellationToken>()));
        _generalSettingsOptions.Value.ExternalAppBaseUrl = "https://{org}.apps.{hostName}/{org}/{app}";

        var target = SetupPdfService(
            pdfGeneratorClient: _pdfGeneratorClient,
            generalSettingsOptions: _generalSettingsOptions,
            telemetrySink: telemetrySink
        );

        Instance instance =
            new()
            {
                Id = $"509378/{Guid.NewGuid()}",
                AppId = "digdir/not-really-an-app",
                Org = "digdir"
            };

        // Act
        await target.GenerateAndStorePdf(instance, "Task_1", CancellationToken.None);

        // Asserts
        _pdfGeneratorClient.Verify(
            s =>
                s.GeneratePdf(
                    It.Is<Uri>(u =>
                        u.Scheme == "https"
                        && u.Host == $"{instance.Org}.apps.{HostName}"
                        && u.AbsoluteUri.Contains(instance.AppId)
                        && u.AbsoluteUri.Contains(instance.Id)
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _dataClient.Verify(
            s =>
                s.InsertBinaryData(
                    It.Is<string>(s => s == instance.Id),
                    It.Is<string>(s => s == "ref-data-as-pdf"),
                    It.Is<string>(s => s == "application/pdf"),
                    It.Is<string>(s => s == "not-really-an-app.pdf"),
                    It.IsAny<Stream>(),
                    It.Is<string>(s => s == "Task_1")
                ),
            Times.Once
        );

        await Verify(telemetrySink.GetSnapshot());
    }

    [Fact]
    public async Task GenerateAndStorePdf_with_generatedFrom()
    {
        // Arrange
        _pdfGeneratorClient.Setup(s => s.GeneratePdf(It.IsAny<Uri>(), It.IsAny<CancellationToken>()));

        _generalSettingsOptions.Value.ExternalAppBaseUrl = "https://{org}.apps.{hostName}/{org}/{app}";

        var target = SetupPdfService(
            pdfGeneratorClient: _pdfGeneratorClient,
            generalSettingsOptions: _generalSettingsOptions
        );

        var dataModelId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();

        Instance instance =
            new()
            {
                Id = $"509378/{Guid.NewGuid()}",
                AppId = "digdir/not-really-an-app",
                Org = "digdir",
                Process = new() { CurrentTask = new() { ElementId = "Task_1" } },
                Data = new()
                {
                    new() { Id = dataModelId.ToString(), DataType = "Model" },
                    new() { Id = attachmentId.ToString(), DataType = "attachment" }
                }
            };

        // Act
        await target.GenerateAndStorePdf(instance, "Task_1", CancellationToken.None);

        // Asserts
        _pdfGeneratorClient.Verify(
            s =>
                s.GeneratePdf(
                    It.Is<Uri>(u =>
                        u.Scheme == "https"
                        && u.Host == $"{instance.Org}.apps.{HostName}"
                        && u.AbsoluteUri.Contains(instance.AppId)
                        && u.AbsoluteUri.Contains(instance.Id)
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _dataClient.Verify(
            s =>
                s.InsertBinaryData(
                    It.Is<string>(s => s == instance.Id),
                    It.Is<string>(s => s == "ref-data-as-pdf"),
                    It.Is<string>(s => s == "application/pdf"),
                    It.Is<string>(s => s == "not-really-an-app.pdf"),
                    It.IsAny<Stream>(),
                    It.Is<string>(s => s == "Task_1")
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetLanguage_ShouldReturnLanguageFromHttpContext()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Append("Accept-Language", LanguageConst.Bokmål);
        _httpContextAccessor.Setup(s => s.HttpContext!).Returns(httpContext);

        var target = SetupPdfService(httpContentAccessor: _httpContextAccessor);

        // Act
        var language = await target.GetLanguage(httpContext);

        // Assert
        language.Should().Be(LanguageConst.Bokmål);
    }

    [Fact]
    public async Task GetLanguage_NoLanguageInHttpContext_ShouldReturnBokmål()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        _httpContextAccessor.Setup(s => s.HttpContext!).Returns(httpContext);

        var target = SetupPdfService(httpContentAccessor: _httpContextAccessor);

        // Act
        var language = await target.GetLanguage(httpContext);

        // Assert
        language.Should().Be(LanguageConst.Bokmål);
    }

    [Fact]
    public async Task GetLanguage_HttpContextIsNull_ShouldReturnBokmål()
    {
        // Arrange
        _httpContextAccessor.Setup(s => s.HttpContext).Returns(null as HttpContext);

        var target = SetupPdfService(httpContentAccessor: _httpContextAccessor);

        // Act
        var language = await target.GetLanguage(null);

        // Assert
        language.Should().Be(LanguageConst.Bokmål);
    }

    [Fact]
    public async Task GetLanguage_UserProfileIsNull_ShouldThrow()
    {
        // Arrange
        var userId = 123;

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new(AltinnCoreClaimTypes.UserId, userId.ToString())], "TestAuthType")
            )
        };

        _profile.Setup(s => s.GetUserProfile(It.IsAny<int>())).Returns(Task.FromResult<UserProfile?>(null));

        var target = SetupPdfService(profile: _profile);

        // Act
        var func = async () => await target.GetLanguage(httpContext);

        // Assert
        await func.Should().ThrowAsync<Exception>().WithMessage("Could not get user profile while getting language");
    }

    [Fact]
    public void GetOverridenLanguage_ShouldReturnLanguageFromQuery()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Request.Query = new QueryCollection(
            new Dictionary<string, StringValues> { { "lang", LanguageConst.Bokmål } }
        );

        // Act
        var language = PdfService.GetOverriddenLanguage(httpContext);

        // Assert
        language.Should().Be(LanguageConst.Bokmål);
    }

    [Fact]
    public void GetOverridenLanguage_HttpContextIsNull_ShouldReturnNull()
    {
        // Arrange
        HttpContext? httpContext = null;

        // Act
        var language = PdfService.GetOverriddenLanguage(httpContext);

        // Assert
        language.Should().BeNull();
    }

    [Fact]
    public void GetOverridenLanguage_NoLanguageInQuery_ShouldReturnNull()
    {
        // Arrange
        DefaultHttpContext httpContext = new();

        // Act
        var language = PdfService.GetOverriddenLanguage(httpContext);

        // Assert
        language.Should().BeNull();
    }

    private PdfService SetupPdfService(
        Mock<IAppResources>? appResources = null,
        Mock<IDataClient>? dataClient = null,
        Mock<IHttpContextAccessor>? httpContentAccessor = null,
        Mock<IProfileClient>? profile = null,
        Mock<IPdfGeneratorClient>? pdfGeneratorClient = null,
        IOptions<PdfGeneratorSettings>? pdfGeneratorSettingsOptions = null,
        IOptions<GeneralSettings>? generalSettingsOptions = null,
        TelemetrySink? telemetrySink = null
    )
    {
        return new PdfService(
            appResources?.Object ?? _appResources.Object,
            dataClient?.Object ?? _dataClient.Object,
            httpContentAccessor?.Object ?? _httpContextAccessor.Object,
            profile?.Object ?? _profile.Object,
            pdfGeneratorClient?.Object ?? _pdfGeneratorClient.Object,
            pdfGeneratorSettingsOptions ?? _pdfGeneratorSettingsOptions,
            generalSettingsOptions ?? _generalSettingsOptions,
            telemetrySink?.Object
        );
    }
}

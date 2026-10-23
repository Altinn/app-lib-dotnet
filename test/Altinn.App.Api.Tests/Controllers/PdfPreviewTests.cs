using System.Text;
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Texts;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Api.Tests.Controllers;

public sealed class PdfPreviewTests : IDisposable
{
    private const string MainId = "7b5a1f0e-6d8c-4a3b-9e2f-1c4d5e6f7a8b";
    private const string FirstSubformId = "3c9e2d1f-8a7b-4c6d-9e5f-0a1b2c3d4e5f";
    private const string SecondSubformId = "bc6ad46b-1428-4ab8-b787-592a67a70bfa";
    private readonly Guid _instanceGuid = Guid.NewGuid();
    private readonly Mock<IInstanceClient> _instances = new();
    private readonly Mock<IProcessReader> _process = new();
    private readonly Mock<IDataClient> _data = new(MockBehavior.Strict);
    private readonly Mock<IPdfGeneratorClient> _generator = new();
    private readonly Mock<ITranslationService> _texts = new();
    private readonly Mock<IPdfService> _legacy = new(MockBehavior.Strict);
    private readonly MemoryStream _pdf = new(Encoding.UTF8.GetBytes("%PDF-preview"));
    private readonly ServiceProvider _services;
    private readonly PdfController _controller;
    private readonly Instance _instance;
    private Uri? _renderedUrl;
    private string? _footer;
    private CancellationToken _renderCancellation;

    public PdfPreviewTests()
    {
        _instance = new()
        {
            Org = "org",
            AppId = "org/app",
            Id = $"12345/{_instanceGuid}",
            Process = new() { CurrentTask = new() { ElementId = "Task_1" } },
            Data =
            [
                new() { Id = MainId, DataType = "main" },
                new() { Id = FirstSubformId, DataType = "subform" },
                new() { Id = SecondSubformId, DataType = "subform" },
            ],
        };
        _instances
            .Setup(x => x.GetInstance("app", "org", 12345, _instanceGuid, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_instance);
        _generator
            .Setup(x => x.GeneratePdf(It.IsAny<Uri>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Uri, string?, CancellationToken>(
                (uri, footer, token) =>
                {
                    _renderedUrl = uri;
                    _footer = footer;
                    _renderCancellation = token;
                }
            )
            .ReturnsAsync(_pdf);
        _texts.Setup(x => x.TranslateTextKey("pdfPreviewText", It.IsAny<string>())).ReturnsAsync("PREVIEW");
        var authentication = new Mock<IAuthenticationContext>();
        authentication
            .Setup(x => x.Current)
            .Returns(
                TestAuthentication.GetUserAuthentication(
                    profileSettingPreference: new ProfileSettingPreference { Language = "nn" }
                )
            );
        var httpContext = new DefaultHttpContext();
        var preview = new PdfService(
            _data.Object,
            new HttpContextAccessor { HttpContext = httpContext },
            _generator.Object,
            Options.Create(new PdfGeneratorSettings()),
            Options.Create(new GeneralSettings { HostName = "local.altinn.cloud" }),
            NullLogger<PdfService>.Instance,
            authentication.Object,
            _texts.Object,
            Mock.Of<IAppResources>()
        );
        _services = new ServiceCollection()
            .AddSingleton(_process.Object)
            .AddSingleton<IPdfPreviewService>(preview)
            .BuildServiceProvider();
        httpContext.RequestServices = _services;
        _controller = new PdfController(
            _instances.Object,
            Mock.Of<IPdfFormatter>(),
            Mock.Of<IAppResources>(),
            Mock.Of<IAppModel>(),
            _data.Object,
            _legacy.Object
        )
        {
            ControllerContext = new() { HttpContext = httpContext },
        };
        _process
            .Setup(x => x.GetFlowElement("Pdf"))
            .Returns(
                new ServiceTask
                {
                    Id = "Pdf",
                    ExtensionElements = new()
                    {
                        TaskExtension = new()
                        {
                            TaskType = "pdf",
                            PdfConfiguration = new() { AutoPdfTaskIds = ["Task_1", "Task_2"] },
                        },
                    },
                }
            );
        _process
            .Setup(x => x.GetFlowElement("SubformPdf"))
            .Returns(
                new ServiceTask
                {
                    Id = "SubformPdf",
                    ExtensionElements = new()
                    {
                        TaskExtension = new()
                        {
                            TaskType = "subformPdf",
                            PdfConfiguration = new() { AutoPdfTaskIds = ["Task_1"] },
                            SubformPdfConfiguration = new()
                            {
                                SubformComponentId = "subform-component",
                                SubformDataTypeId = "subform",
                            },
                        },
                    },
                }
            );
        _process
            .Setup(x => x.GetFlowElement("Task_1"))
            .Returns(
                new ProcessTask
                {
                    Id = "Task_1",
                    ExtensionElements = new() { TaskExtension = new() { TaskType = "data" } },
                }
            );
    }

    [Fact]
    public async Task FuturePdfTaskUsesItsConfigurationWithoutStoringOrAdvancing()
    {
        using var cancellation = new CancellationTokenSource();
        var result = Assert.IsType<FileStreamResult>(await Preview("Pdf", token: cancellation.Token));
        Assert.Same(_pdf, result.FileStream);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Contains($"/#/instance/12345/{_instanceGuid}/Pdf/?pdf=1&lang=nn", _renderedUrl!.AbsoluteUri);
        Assert.Contains("task=Task_1", _renderedUrl.AbsoluteUri);
        Assert.Contains("task=Task_2", _renderedUrl.AbsoluteUri);
        Assert.Contains("PREVIEW", _footer);
        Assert.Equal(cancellation.Token, _renderCancellation);
        _instances.Verify(x => x.GetInstance("app", "org", 12345, _instanceGuid, null, cancellation.Token), Times.Once);
        Assert.Equal("Task_1", _instance.Process.CurrentTask.ElementId);
        Assert.Equal(3, _instance.Data.Count);
        _data.VerifyNoOtherCalls();
        _legacy.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(FirstSubformId)]
    [InlineData(SecondSubformId)]
    public async Task SubformPreviewRendersOnlyTheSelectedElement(string id)
    {
        Assert.IsType<FileStreamResult>(await Preview("SubformPdf", Guid.Parse(id)));
        Assert.Contains($"/SubformPdf/subform/subform-component/{id}/?pdf=1", _renderedUrl!.AbsoluteUri);
        Assert.DoesNotContain("task=", _renderedUrl.AbsoluteUri);
        _data.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(MainId)]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task MissingOrWrongSubformIsRejectedBeforeRendering(string? id)
    {
        Assert.IsType<BadRequestObjectResult>(await Preview("SubformPdf", id is null ? null : Guid.Parse(id)));
        Assert.Null(_renderedUrl);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData(null, "nn")]
    [InlineData("", "nn")]
    public async Task PreviewUsesRequestedLanguageOrUserLanguage(string? language, string expected)
    {
        Assert.IsType<FileStreamResult>(await Preview("Pdf", language: language));
        Assert.Contains($"lang={expected}", _renderedUrl!.AbsoluteUri);
        _texts.Verify(x => x.TranslateTextKey("pdfPreviewText", expected), Times.Once);
    }

    [Fact]
    public async Task UnknownTaskReturnsNotFound()
    {
        Assert.IsType<NotFoundObjectResult>(await Preview("Unknown"));
        Assert.Null(_renderedUrl);
    }

    [Fact]
    public async Task NonPdfTaskReturnsBadRequest()
    {
        Assert.IsType<BadRequestObjectResult>(await Preview("Task_1"));
        Assert.Null(_renderedUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Pdf")]
    public async Task InstanceWithoutCurrentTaskReturnsNotFound(string? taskId)
    {
        _instance.Process.CurrentTask = null;
        Assert.IsType<NotFoundObjectResult>(await Preview(taskId));
        Assert.Null(_renderedUrl);
    }

    [Fact]
    public async Task MissingInstanceReturnsNotFound()
    {
        _instances
            .Setup(x => x.GetInstance("app", "org", 12345, _instanceGuid, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instance)null!);
        Assert.IsType<NotFoundObjectResult>(await Preview("Pdf"));
    }

    [Fact]
    public async Task PreviewWithoutTaskIdPreservesAppProvidedPdfService()
    {
        _legacy.Setup(x => x.GeneratePdf(_instance, "Task_1", true, CancellationToken.None)).ReturnsAsync(_pdf);
        var result = Assert.IsType<FileStreamResult>(await Preview(null));
        Assert.Same(_pdf, result.FileStream);
        Assert.Null(_renderedUrl);
        _process.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingSubformConfigurationIsReported()
    {
        _process
            .Setup(x => x.GetFlowElement("Bad"))
            .Returns(
                new ServiceTask
                {
                    Id = "Bad",
                    ExtensionElements = new() { TaskExtension = new() { TaskType = "subformPdf" } },
                }
            );
        await Assert.ThrowsAsync<ApplicationConfigException>(() => Preview("Bad", Guid.Parse(FirstSubformId)));
        Assert.Null(_renderedUrl);
    }

    private Task<ActionResult> Preview(
        string? taskId,
        Guid? id = null,
        string? language = null,
        CancellationToken token = default
    ) => _controller.GetPdfPreview("org", "app", 12345, _instanceGuid, taskId, id, language, token);

    public void Dispose()
    {
        _services.Dispose();
        _pdf.Dispose();
    }
}

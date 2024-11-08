using System.Net;
using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Features.Correspondence.Builder;
using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Altinn.App.Core.Tests.Features.Correspondence;

public class CorrespondenceClientTests
{
    private sealed record Fixture(WebApplication App) : IAsyncDisposable
    {
        public Mock<IHttpClientFactory> HttpClientFactoryMock =>
            Mock.Get(App.Services.GetRequiredService<IHttpClientFactory>());

        public CorrespondenceClient CorrespondenceClient =>
            (CorrespondenceClient)App.Services.GetRequiredService<ICorrespondenceClient>();

        public static Fixture Create()
        {
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var app = Api.Tests.TestUtils.AppBuilder.Build(registerCustomAppServices: services =>
            {
                services.AddSingleton(mockHttpClientFactory.Object);
            });

            return new Fixture(app);
        }

        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    private static SendCorrespondencePayload PayloadFactory()
    {
        return SendCorrespondencePayload.WithBuilder(
            CorrespondenceRequestBuilder
                .Create()
                .WithResourceId("resource-id")
                .WithSender(OrganisationNumber.Parse("991825827"))
                .WithSendersReference("senders-ref")
                .WithRecipient(OrganisationNumber.Parse("213872702"))
                .WithDueDateTime(DateTime.Now.AddMonths(6))
                .WithAllowSystemDeleteAfter(DateTime.Now.AddYears(1))
                .WithContent(
                    CorrespondenceContentBuilder
                        .Create()
                        .WithTitle("message-title")
                        .WithLanguage(LanguageCode<Iso6391>.Parse("en"))
                        .WithSummary("message-summary")
                        .WithBody("message-body")
                ),
            async () =>
                await Task.FromResult(
                    AccessToken.Parse(
                        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJpdHMtYS1tZSJ9.wLLw4Timcl9gnQvA93RgREz-6S5y1UfzI_GYVI_XVDA"
                    )
                )
        );
    }

    [Fact]
    public async Task Send_SuccessfulResponse_ReturnsCorrespondenceResponse()
    {
        // Arrange
        await using var fixture = Fixture.Create();
        var mockHttpClientFactory = fixture.HttpClientFactoryMock;
        var mockHttpClient = new Mock<HttpClient>();

        var payload = PayloadFactory();
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                    "correspondences": [
                        {
                            "correspondenceId": "cf7a4a9f-45ce-46b9-b110-4f263b395842",
                            "status": "Initialized",
                            "recipient": "0192:213872702",
                            "notifications": [
                                {
                                    "orderId": "05119865-6d46-415c-a2d7-65b9cd173e13",
                                    "isReminder": false,
                                    "status": "Success"
                                }
                            ]
                        }
                    ],
                    "attachmentIds": [
                        "25b87c22-e7cc-4c07-95eb-9afa32e3ee7b"
                    ]
                }
                """
            )
        };

        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);
        mockHttpClient
            .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMessage);

        // Act
        var result = await fixture.CorrespondenceClient.Send(payload);

        // Assert
        Assert.NotNull(result);
        result.AttachmentIds.Should().ContainSingle("25b87c22-e7cc-4c07-95eb-9afa32e3ee7b");
        result.Correspondences.Should().HaveCount(1);
        result.Correspondences[0].CorrespondenceId.Should().Be("cf7a4a9f-45ce-46b9-b110-4f263b395842");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Send_FailedResponse_ThrowsCorrespondenceRequestException(HttpStatusCode httpStatusCode)
    {
        // Arrange
        await using var fixture = Fixture.Create();
        var mockHttpClientFactory = fixture.HttpClientFactoryMock;
        var mockHttpClient = new Mock<HttpClient>();

        var payload = PayloadFactory();
        var responseMessage = new HttpResponseMessage(httpStatusCode)
        {
            Content = httpStatusCode switch
            {
                HttpStatusCode.BadRequest
                    => new StringContent(
                        """
                        {
                            "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                            "title": "Bad Request",
                            "status": 400,
                            "detail": "For upload requests at least one attachment has to be included",
                            "traceId": "00-3ceaba074547008ac46f622fd67d6c6e-e4129c2b46370667-00"
                        }
                        """
                    ),
                _ => null
            }
        };

        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);
        mockHttpClient
            .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseMessage);

        // Act
        Func<Task> act = async () =>
        {
            await fixture.CorrespondenceClient.Send(payload);
        };

        // Assert
        await act.Should().ThrowAsync<CorrespondenceRequestException>();
    }

    [Fact]
    public async Task Send_UnexpectedException_IsHandled()
    {
        // Arrange
        await using var fixture = Fixture.Create();
        var mockHttpClientFactory = fixture.HttpClientFactoryMock;
        var mockHttpClient = new Mock<HttpClient>();
        var payload = PayloadFactory();

        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);
        mockHttpClient
            .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => throw new HttpRequestException("Surprise!"));

        // Act
        Func<Task> act = async () =>
        {
            await fixture.CorrespondenceClient.Send(payload);
        };

        // Assert
        await act.Should()
            .ThrowAsync<CorrespondenceRequestException>()
            .WithInnerExceptionExactly(typeof(HttpRequestException));
    }
}

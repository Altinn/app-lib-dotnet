using Altinn.App.Core.Features.Correspondence;
using Altinn.App.Core.Models;
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

        public static Fixture Create()
        {
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            var app = Api.Tests.TestUtils.AppBuilder.Build(registerCustomAppServices: services =>
            {
                services.AddSingleton(mockHttpClientFactory.Object);
                // services.Configure<MemoryCacheOptions>(options => options.Clock = fakeTimeProvider);
                // services.AddSingleton(fakeTimeProvider);

                // services.AddCorrespondenceClient();
            });

            return new Fixture(app);
        }

        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    [Fact]
    public async Task Test()
    {
        // Arrange
        await using var fixture = Fixture.Create();
    }

    // [Fact]
    // public void Test_Message_Builder()
    // {
    //     var messageBuilder = new CorrespondenceMessageBuilder(
    //         "12345678901",
    //         OrganisationNumber.Parse("991825827"),
    //         "test-ref",
    //         new MessageContentBuilder("Tittel", "norsk", "Summary", "Body"),
    //         ["0912:991825827"]
    //     );

    //     messageBuilder = messageBuilder.WithContentAttachments(
    //         [
    //             new CorrespondenceAttachment(
    //                 "file.pdf",
    //                 "file",
    //                 "restriction",
    //                 IsEncrypted: false,
    //                 "sender",
    //                 "senders-ref",
    //                 "datatype",
    //                 "dataLocationType"
    //             )
    //         ]
    //     );

    //     var message = messageBuilder.Build();
    //     Assert.NotNull(message);
    // }
}

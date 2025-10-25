using System.Text.Json;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using KS.Fiks.IO.Client.Models;
using Moq;
using RabbitMQ.Client.Events;
using ExternalFiksIOConfiguration = KS.Fiks.IO.Client.Configuration.FiksIOConfiguration;

namespace Altinn.App.Clients.Fiks.Tests.FiksIO;

public class FiksIOClientTest
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    [Theory]
    [InlineData("Production")]
    [InlineData("AnythingElse")]
    public async Task InitialiseFiksIOClient_CallsCreateClientWithCorrectPayload_Default(string environment)
    {
        // Arrange
        var externalFiksIOClientMock = new Mock<KS.Fiks.IO.Client.IFiksIOClient>();
        ExternalFiksIOConfiguration? capturedConfig = null;
        await using var fixture = TestFixture.Create(services =>
        {
            services.AddFiksIOClient();
        });

        fixture.HostEnvironmentMock.Setup(x => x.EnvironmentName).Returns(environment);
        fixture
            .FiksIOClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<ExternalFiksIOConfiguration>()))
            .ReturnsAsync(
                (ExternalFiksIOConfiguration config) =>
                {
                    capturedConfig = config;
                    return externalFiksIOClientMock.Object;
                }
            )
            .Verifiable(Times.Once);

        // Act
        var result = await ((FiksIOClient)fixture.FiksIOClient).InitialiseFiksIOClient();

        // Assert
        Assert.Same(externalFiksIOClientMock.Object, result);
        Assert.NotNull(capturedConfig);
        fixture.FiksIOClientFactoryMock.Verify();

        await Verify(
                JsonSerializer.Serialize(FiksIOConfigurationExtract.Create(capturedConfig), _jsonSerializerOptions)
            )
            .UseDefaultSettings(environment);
    }

    [Theory]
    [InlineData(1, "https://custom-api.fiks.test", "amqp://custom-amqp.fiks.test")]
    [InlineData(2, "http://custom-api.fiks.test", "ignored://custom-amqp.fiks.test")]
    [InlineData(3, "https://custom-api.fiks.test:1234", "amqp://custom-amqp.fiks.test:1234")]
    public async Task InitialiseFiksIOClient_CallsCreateClientWithCorrectPayload_Custom(
        int testId,
        string apiHost,
        string amqpHost
    )
    {
        // Arrange
        var fiksIOSettings = new FiksIOSettings
        {
            AccountId = Guid.Parse("2c83fc27-83da-4bc8-8d9c-d45e20d670f3"),
            IntegrationId = Guid.Parse("60646af0-187d-4533-87a8-50da7af8e8f1"),
            IntegrationPassword = "integration-passord",
            ApiHost = apiHost,
            AmqpHost = amqpHost,
            AccountPrivateKeyBase64 =
                "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSUV2UUlCQURBTkJna3Foa2lHOXcwQkFRRUZBQVNDQktjd2dnU2pBZ0VBQW9JQkFRQ29ybHdwS09QWmVRQWYKeXhXc1I5WUZFZVZaWGFVYUVNb29vdU9BM0VzSzVDUUpJWTBXak5EZ1pTR25VbVMydDFiNWVCTExENzRxbHN0UQo4cU1aVTlmUi9YMFpFM2ZwQXZFcXg0M0hPTXRna0M4UHhqc0JKVkpEM0FZM0h5WEs3UkkyT0hzenF5NWdMem1ZClIvVFN2Z2FNSm9zaVNoY2ZVaWthMkFMVXJ2ZzdlSTh0SkhFV3VyaXdQVE5PTXVhcjhkMHIrTTh3SnJDTlhBVUUKNW80aWFhNSt5aTRZdW5tbHRsSjZQTEtZekJUQnBtOE1Wcnd6ZEt2SnBlQ2RNaGYvaWNUS04vakJEOXNZMjJrUgppSVEyZXN1SUFQLzkyQk1wTDNIMzlTT3lRY3ZZUGJWek9ZK0ZONDB0VzFrSnNiZTRBMHlqbGRUdFdySThsS0VLCkJDaUY1U1RyQWdNQkFBRUNnZ0VBVVo1a2k2bjBiZ01sY3p6U0ZOV0JrbHh4YkVkcW51MG0xSGpVdWtKZHNUeG8KejE1RHBYaW5qUnlDSnpNdjlsVktLVlhYU2hnby9QU1FGbkFCU1QwUGZkVTVGY3djeGt1ZEpBVUE3amZsK0tocgpqU2R1MzRRT2hUVk9jSmVLc3VENVlmRDhkQ2pOUE04OUFCazVOa3VxWVdBNURITnJNc1dQakNTZllnZnpwdHBICnlTWS9JR1MrYlJSU2JjN2lDYlQ0ZU5OM2ViNmFHa0U4MXMydlltM1B0cnE0b1ZCRW9UOHNUdEFNRCthVE53SzUKUnI2OWtDL0VLSEdVTlI3N2YwZll1SUU3MGxFZHdPZnQ1YTNHZXVLSnI3U0xXUmJRTnN6NTRzdkMzTUtuMjBLMwp4RmYrbk9vcGd6WDJEOUxNeFphMk5hcGovY0Uzc3JxQnBNWnJRMis1S1FLQmdRRDNid01sSURvL1F2KzRiYm5QCkJwSmY5eVk1QnJybzVaYTZwNlFqcVh3OXp5ZVpQTlBMMnp5VHNFSlVPamlrSXdjWUFlVEZSQmpHOHJRdWZldUQKclJ2SmdiWld4MDlqOUptN3daWlV5dEtualJTdm5iSnVnWFFvU3lncUxNVE1oa3ZxcTJnOFhERDliWktVclFlVAp1aGY0L3FQZFJxVm13UkVaenEzZjUxQ01GUUtCZ1FDdWhWNkM2UGJ5R2h0OUt5TUhRTk1xRXJzd1dEWUI2eTlOCm0vay81eXRvRzdoQkhFQkZ3RTRudUlOd2hwekhndjFHS0JTbnllNG45TUp1aGFXUGEydXFKYzBpMEpTYXpyd1cKamN1VVRXWStydkowLzB1STg3eUtwOXJXc05ISU9TRyt0Z2Jqa2JWM09IeU9mRERBWkt6bWkwS3F2VXRrQ21zegptaTFVV2I0cy93S0JnSFAyd0ZlWENoVjZGaFZldjVXcWM5R3NLR3JUbjZmbGFrWWlxWWZaT0JSVDBUQTlWTm9XCjd3WWN0b2w1Ry9DNVJlclhnRnQvdGNrUUdLZ3RkY0twei9zWjR0WTY1UmRvbk9CbE0wcWNJQ3hNcjZRNjJWUjMKTVhScy9PNy9PbURNbENud21aS29kb0ZmNkRIc0FrQVhHSU1tL0srSkc1M1Q0R3JpOVpTWmVhT2RBb0dBTEZ5awppSnhWWVhjM2M1cktmYWFCeHRrYUVoRHFNWlNwNG8xNlR6OGc3b2JWVUYreENzbjJVK3g0Sk50aThPZ2dIM3hDCnU5LzlsTlNIcy9adS9rVHRDa1F2bitEclZXcWxyd24rTE1WNzd3VWpoby82a0daOGo3WlpUMmVFKytOaDY0dHoKdHRjc0RKRWtRRWZiVUp5R3d0ZTFhNWRRbWE1NFQ1YXFYUjJ1MU5zQ2dZRUFvSkorSlIyUjNOQmtIbFQ2NXN2MwpwSnNTN3BDQjRSNlNxS0p4SnF2TGdWdWNQSFlZZVlOb1ZwRWVXRTlNV1I5VHZCWnNrbUpVUERManBrN29aNXJGCnVKL2lSbGtHYWczTEhOa1VpQy9TWXFCNERkTlcyY2J4ekJCUmxOUGkzcGZTVHNGV2hNUHZ4S0VTaHhuRC9FV3EKek9jcWwzYmRRQmlTZW9aWEVsNDY1Rjg9Ci0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0=",
        };
        var externalFiksIOClientMock = new Mock<KS.Fiks.IO.Client.IFiksIOClient>();
        ExternalFiksIOConfiguration? capturedConfig = null;
        await using var fixture = TestFixture.Create(
            services =>
            {
                services.AddFiksIOClient().WithFiksIOConfig("CustomFiksIOSettings");
            },
            [("CustomFiksIOSettings", fiksIOSettings)]
        );

        fixture
            .FiksIOClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<ExternalFiksIOConfiguration>()))
            .ReturnsAsync(
                (ExternalFiksIOConfiguration config) =>
                {
                    capturedConfig = config;
                    return externalFiksIOClientMock.Object;
                }
            )
            .Verifiable(Times.Once);

        // Act
        var result = await ((FiksIOClient)fixture.FiksIOClient).InitialiseFiksIOClient();

        // Assert
        Assert.Same(externalFiksIOClientMock.Object, result);
        Assert.NotNull(capturedConfig);
        fixture.FiksIOClientFactoryMock.Verify();

        await Verify(
                JsonSerializer.Serialize(FiksIOConfigurationExtract.Create(capturedConfig), _jsonSerializerOptions)
            )
            .UseDefaultSettings(testId);
    }

    [Fact]
    public async Task InitialiseFiksIOClient_DisposesAndSubscribesToEvents()
    {
        // Arrange
        var externalFiksIOClientMock = new Mock<KS.Fiks.IO.Client.IFiksIOClient>();
        var fixture = TestFixture.Create(services => // Don't dispose the fixture here, it messes with the verifications
        {
            services.AddFiksIOClient();
        });

        fixture
            .FiksIOClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<ExternalFiksIOConfiguration>()))
            .ReturnsAsync(externalFiksIOClientMock.Object)
            .Verifiable(Times.Exactly(2));
        externalFiksIOClientMock.Setup(x => x.DisposeAsync()).Verifiable(Times.Once);
        externalFiksIOClientMock
            .Setup(x =>
                x.NewSubscriptionAsync(
                    It.IsAny<Func<MottattMeldingArgs, Task>>(),
                    It.IsAny<Func<ConsumerEventArgs, Task>>()
                )
            )
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(2));

        // Act
        await fixture.FiksIOClient.OnMessageReceived(_ => Task.CompletedTask);
        await ((FiksIOClient)fixture.FiksIOClient).InitialiseFiksIOClient();

        // Assert
        fixture.FiksIOClientFactoryMock.Verify();
        externalFiksIOClientMock.Verify();
    }

    private sealed record FiksIOConfigurationExtract
    {
        public required string ApiScheme { get; init; }
        public required string ApiHost { get; init; }
        public required int ApiPort { get; init; }
        public required string AmqpHost { get; init; }
        public required int AmqpPort { get; init; }
        public required string ApplicationName { get; init; }
        public required int PrefetchCount { get; init; }
        public required Guid IntegrationId { get; init; }
        public required string IntegrationPassord { get; init; }
        public required string IntegrationScope { get; init; }
        public required Guid AccountId { get; init; }
        public required IEnumerable<string> AccountPrivateKey { get; init; }

        public static FiksIOConfigurationExtract Create(ExternalFiksIOConfiguration config) =>
            new()
            {
                ApiScheme = config.ApiConfiguration.Scheme,
                ApiHost = config.ApiConfiguration.Host,
                ApiPort = config.ApiConfiguration.Port,
                AmqpHost = config.AmqpConfiguration.Host,
                AmqpPort = config.AmqpConfiguration.Port,
                ApplicationName = config.AmqpConfiguration.ApplicationName,
                PrefetchCount = config.AmqpConfiguration.PrefetchCount,
                IntegrationId = config.IntegrasjonConfiguration.IntegrasjonId,
                IntegrationPassord = config.IntegrasjonConfiguration.IntegrasjonPassord,
                IntegrationScope = config.IntegrasjonConfiguration.Scope,
                AccountId = config.KontoConfiguration.KontoId,
                AccountPrivateKey = config.KontoConfiguration.PrivatNokler.Select(x => x[..50] + "..."),
            };
    };
}

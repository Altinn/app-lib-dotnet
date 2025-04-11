using System.Net;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.Platform.Storage.Interface.Models;
using Moq;
using Moq.Protected;

namespace Altinn.App.Clients.Fiks.Tests;

internal static class TestHelpers
{
    public static HttpClient GetHttpClientWithMockedHandler(
        HttpStatusCode statusCode,
        Func<HttpRequestMessage, string?>? contentFactory = null,
        Action<HttpRequestMessage>? requestCallback = null
    )
    {
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                (HttpRequestMessage request, CancellationToken _) =>
                {
                    requestCallback?.Invoke(request);

                    var content = contentFactory?.Invoke(request);
                    return new HttpResponseMessage(statusCode)
                    {
                        Content = content is not null ? new StringContent(content) : null,
                    };
                }
            );

        return new HttpClient(mockHttpMessageHandler.Object);
    }

    public static Func<HttpClient> GetHttpClientWithMockedHandlerFactory(
        HttpStatusCode statusCode,
        Func<HttpRequestMessage, string?>? contentFactory = null,
        Action<HttpRequestMessage>? requestCallback = null
    )
    {
        return () => GetHttpClientWithMockedHandler(statusCode, contentFactory, requestCallback);
    }

    public static MaskinportenSettings GetDefaultMaskinportenSettings()
    {
        return new MaskinportenSettings
        {
            Authority = "test-authority",
            ClientId = "test-client-id",
            JwkBase64 = "test-jwk-base64",
        };
    }

    public static FiksIOSettings GetDefaultFiksIOSettings()
    {
        return new FiksIOSettings
        {
            AccountId = Guid.Empty,
            IntegrationId = Guid.Empty,
            IntegrationPassword = "test-integration-password",
            AccountPrivateKeyBase64 = "test-account-pk-base64",
            AsicePrivateKeyBase64 = "test-asice-pk-base64",
        };
    }

    public static FiksArkivSettings GetDefaultFiksArkivSettings()
    {
        return new FiksArkivSettings
        {
            Receipt = new FiksArkivDataTypeSettings { DataType = "fiks-receipt" },
            Recipient = new FiksArkivRecipientSettings
            {
                FiksAccount = new FiksArkivRecipientValue<Guid?>
                {
                    DataModelBinding = new FiksArkivDataModelBinding { DataType = "model", Field = "recipient" },
                },
                Identifier = new FiksArkivRecipientValue<string> { Value = Guid.NewGuid().ToString() },
                OrganizationNumber = new FiksArkivRecipientValue<string> { Value = Guid.NewGuid().ToString() },
                Name = new FiksArkivRecipientValue<string> { Value = Guid.NewGuid().ToString() },
            },
            AutoSend = new FiksArkivAutoSendSettings
            {
                AfterTaskId = "Task_1",
                SuccessHandling = new FiksArkivSuccessHandlingSettings
                {
                    MoveToNextTask = true,
                    MarkInstanceComplete = true,
                },
                ErrorHandling = new FiksArkivErrorHandlingSettings
                {
                    MoveToNextTask = true,
                    SendEmailNotifications = true,
                    EmailNotificationRecipients = ["someone@somewhere.com"],
                },
            },
            Documents = new FiksArkivDocumentSettings
            {
                PrimaryDocument = new FiksArkivDataTypeSettings
                {
                    DataType = "ref-data-as-pdf",
                    Filename = "formdata.pdf",
                },
                Attachments =
                [
                    new FiksArkivDataTypeSettings { DataType = "model", Filename = "formdata.xml" },
                    new FiksArkivDataTypeSettings { DataType = "uploaded_attachment" },
                ],
            },
        };
    }

    public static MaskinportenSettings GetRandomMaskinportenSettings()
    {
        return new MaskinportenSettings
        {
            Authority = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString(),
            JwkBase64 = Guid.NewGuid().ToString(),
        };
    }

    public static FiksIOSettings GetRandomFiksIOSettings()
    {
        return new FiksIOSettings
        {
            AccountId = Guid.NewGuid(),
            IntegrationId = Guid.NewGuid(),
            IntegrationPassword = Guid.NewGuid().ToString(),
            AccountPrivateKeyBase64 = Guid.NewGuid().ToString(),
            AsicePrivateKeyBase64 = Guid.NewGuid().ToString(),
        };
    }

    public static FiksArkivSettings GetRandomFiksArkivSettings()
    {
        return new FiksArkivSettings
        {
            Recipient = new FiksArkivRecipientSettings
            {
                FiksAccount = new FiksArkivRecipientValue<Guid?>
                {
                    DataModelBinding = new FiksArkivDataModelBinding
                    {
                        DataType = Guid.NewGuid().ToString(),
                        Field = Guid.NewGuid().ToString(),
                    },
                },
                Identifier = new FiksArkivRecipientValue<string> { Value = Guid.NewGuid().ToString() },
                OrganizationNumber = new FiksArkivRecipientValue<string> { Value = Guid.NewGuid().ToString() },
                Name = new FiksArkivRecipientValue<string> { Value = Guid.NewGuid().ToString() },
            },
            Receipt = new FiksArkivDataTypeSettings { DataType = Guid.NewGuid().ToString() },
            AutoSend = new FiksArkivAutoSendSettings
            {
                AfterTaskId = Guid.NewGuid().ToString(),
                ErrorHandling = new FiksArkivErrorHandlingSettings
                {
                    EmailNotificationRecipients = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()],
                },
            },
            Documents = new FiksArkivDocumentSettings
            {
                PrimaryDocument = new FiksArkivDataTypeSettings
                {
                    DataType = Guid.NewGuid().ToString(),
                    Filename = Guid.NewGuid().ToString(),
                },
                Attachments =
                [
                    new FiksArkivDataTypeSettings
                    {
                        DataType = Guid.NewGuid().ToString(),
                        Filename = Guid.NewGuid().ToString(),
                    },
                ],
            },
        };
    }

    public class CustomFiksArkivMessageHandler : IFiksArkivMessageHandler
    {
        public Task ValidateConfiguration(
            IReadOnlyList<DataType> configuredDataTypes,
            IReadOnlyList<ProcessTask> configuredProcessTasks
        )
        {
            throw new NotImplementedException();
        }

        public Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
        {
            throw new NotImplementedException();
        }

        public Task HandleReceivedMessage(Instance instance, FiksIOReceivedMessage receivedMessage)
        {
            throw new NotImplementedException();
        }
    }

    public class CustomAutoSendDecision : IFiksArkivAutoSendDecision
    {
        public Task<bool> ShouldSend(string taskId, Instance instance)
        {
            throw new NotImplementedException();
        }
    }
}

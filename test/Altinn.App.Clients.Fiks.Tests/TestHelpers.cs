using System.Net;
using Altinn.App.Clients.Fiks.FiksArkiv;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Altinn.App.Core.Features.Maskinporten.Models;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client.Models;
using KS.Fiks.IO.Send.Client.Models;
using Moq;
using Moq.Protected;

namespace Altinn.App.Clients.Fiks.Tests;

internal static class TestHelpers
{
    public const string DummyToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    public static HttpClient GetHttpClientWithMockedHandler(
        HttpStatusCode statusCode,
        Func<HttpRequestMessage, string?>? authFactory = null,
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

                    var content = IsTokenRequest(request)
                        ? authFactory?.Invoke(request) ?? DummyToken
                        : contentFactory?.Invoke(request);

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
        Func<HttpRequestMessage, string?>? authFactory = null,
        Func<HttpRequestMessage, string?>? contentFactory = null,
        Action<HttpRequestMessage>? requestCallback = null
    )
    {
        return () => GetHttpClientWithMockedHandler(statusCode, authFactory, contentFactory, requestCallback);
    }

    private static bool IsTokenRequest(HttpRequestMessage request)
    {
        string[] authEndpoints = ["/Home/GetTestOrgToken", "/token", "/exchange/maskinporten"];
        string requestPath = request.RequestUri!.AbsolutePath;

        return authEndpoints.Any(x => requestPath.Contains(x, StringComparison.OrdinalIgnoreCase));
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
            AccountId = Guid.Parse("58f559e2-783e-4b99-9527-8d3342ac4244"),
            IntegrationId = Guid.Parse("930d98f0-57de-4606-a9e8-4a4427249967"),
            IntegrationPassword = "test-integration-password",
            AccountPrivateKeyBase64 =
                "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSUV2Z0lCQURBTkJna3Foa2lHOXcwQkFRRUZBQVNDQktnd2dnU2tBZ0VBQW9JQkFRQ1hnbXd1cHdqL3hoNG4KcDlPQmdUL3dxWEZaQmVEYXpsbFRFT29QQzVMZ1htVU0xa1loRldSVHQrc29iRXZnYU1YcHZmazhTYi9GQ3ZTSgoxT01aV1lnSHo1bFFXY2xFT0RFdjdCUVJoa2JBK3BOQUtZWWdUeFhsbjZzUHl3VHViaVdmOTlQTWNUVWNhangyCmlhTkIrZkRvQnUzaFRTZ1ZrODVseVBiVCs3Y0FCVkVlUVc4eWNHaCtYZ1VsY2R3a3N5WWI1bFN1SkRDUjkrYjAKajI3NFphQjlIUytWbVU1Mi9QWC9wYU5zQkswWlJTQTRSbEd0RFZ5SjJHUG9FQkhEVTM3L09lZGRYQVJVakZmawpjNis3K2JyYmtySDdtcVBtK3lwbm1samk4VDBLOEtuWUlPTDlQbkkzNGZPLy9DZjE1eDJ3Qmovd0p1NmVqMHkvClhlQmgxUDl4QWdNQkFBRUNnZ0VBWmRMc2Urb2NuVEY4SUxDazhCdDZhbmFtUytzc2RFRk1QUXhZRWFaNG5yd3gKODQrcWNCK2RYcnB6bTZZMDFHdjEzeUtpOTRhbEVIdE5YN2lvcStmRkNXTFhLZTQ5MnRCZEZsVDJJOVQzaGtpaApYL1RJUkx5Qi9lSHlLRm9NUldYWGVZd29WdlVhZWE5WVZWNHBUM1Q0R0NoWUJSeEN2VVdwNkRSSTFxME1EMEY2CnU5UWo4dENJYzkySVJFQVMzM0pPZXJxcVcxdmtSZ1hHbkErKzQ3OEtrNUhiWk9HUU5jcmZteFdDTkFmcjNPTkwKNUpadnNIZEh4OGZUNEhsSjV6WmFJV2NaMEFTWVVFMzZQbmRHcFprUWMrWmdZTGVDWS9qeWdtNjBFNlJaM1duYQo1RnM3ODYvQy8zVnc4ZTFOaWFRdkFybWRwRFloZVlYN2NhV1hFRVMzWlFLQmdRREpQN0RUc1p2TDJRL25YSnorCkxxSmU0emtxOXhEQ2F4alE5YmQ2OUlpNlFScFNsTWhnZCtPczVjWE04V0JqUnRQMnhGV0t0c2wvUUhHNFRZczAKalFZTkpxV2dhRHZ6bzNPN0hMREZxcEptQUU4YUVleCtpcEZ6YVhUaWVFNWsxMFBiT2FnZWpzN2dITzh1M3cwNgpJalZZSVVLeUtWOVRiTDdSbjFwRVRqd0pHd0tCZ1FEQXVvMlFLSFBnbFNpcFdXVzJqUnRydkkraFpvMDlUbWxyClYrUUsvTzNsOVN0TEhoZTE2QVNVZmVuM2hMeWtheUpvU2llSjJra0FRTGJMUkZRUDk2eVd5N3VaL1k1ekJKck0KSzdTaURKMjBaSGY5TnJqYzh3ZUFyTmx1QXBKQ1lCZ0JtTkRTSjVBazh6NEwrOEZldCtWbllleTFTSVFYMVk5VQo3MXdTMkRvT1l3S0JnUURGNGh1RVBKcmQyVVNiRVdUSlJwK2Z2N3VCdE1oRTh6dkdsQ1hpLzduRnNxZ29WV1dsCi9aemdjRnFMaHpob3hjYzhXSmRvT3cxc1U3aStLWGxjcGVJeVlqTHZ4QzVYQmZ5UkdzZnl4U01JcXZzY3ZrMFYKckRrVEM3bkR5ZG9EcSt0c0Q0aHc2Nmtka3pYWWw3aVExZnd2K1J4MHhOdVgwMURhRzkrTlZJUVJ5d0tCZ0NnSgpHTXN2ZkJMVktXTTBqT3FGR1lNaDRueFd2MVJTNjVjKzVNSmJsRmZHdkQyWWZMaHZBRFNRaTMrOWRTcDZqdVUzCk1rdHlxdU9BamZoZnMwNjExb1prd0EzWEhEWk1hSk90S0pMWktCR0hKVjNXZGtSL3Y3azlMdFdwZHhTT3ZhM24KUHNuSktpcGkxU3JNRzNrL25rb0JqNWlBL2QrdG4xNjNjbHIveTkrZEFvR0JBS2tCTmtPVzlkN05mOWJOdnNEbAo1TXNVSUpPenNmUDdBSTVvRXFnQ1hVQnhrNEZ0cjVnN25oM1E0NExBSHZXSzFXWkRxcW90VFVadW8yTWlLeDArCk1IbFoxekxTMTB0ck05TnpvSHYyVytUZ0t0YXVxTHl0eUVUWDJadEJlV09CNXhQb0FjS2FuQnlTWCtUbjZ3YkkKV09mSnl6R2M4Vm5rSXMrMzZ5eGZPN3FxCi0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0=",
            AsicePrivateKeyBase64 =
                "LS0tLS1CRUdJTiBQUklWQVRFIEtFWS0tLS0tCk1JSUV2UUlCQURBTkJna3Foa2lHOXcwQkFRRUZBQVNDQktjd2dnU2pBZ0VBQW9JQkFRQ1I5aUsvQUtDRjNLZVAKaUYxYkVXZ0g1NUJsOHp4KzN6RzVQdXdBRWJEbkxrRHdCZEl5d2hncjdqQTRPdGtZZ3JSQmxWazk5RDFOb0tRQQpxQlFoK2JCWmtORDc1aVB6b1RRMzU4NHFHS2ZzaDdsb3JWTXlVdjBHTGFGVlJPU085M1g2U05sY1UySHpzZXpLCk9LanRiWUJ0cDZxMlV1amxmT1pVYjNLMXpqTWtyUmNrRnlybklreVJ0RzBhdHRyL2JUcGNVelkyMEdtcWxkNG4KZ0Y3TXo1ZGo1RmVKQktZZkRRT2J6VXR5dG5WZUdzOWxFQ21QaEtZZmdpdUNjT2tKNGZaUmRZOGFEYUJXbFllOApkREEzdnhUOGJkRzF1em1vbG5wRDk3U3lrL204eDJGZWpOeHNhZEpXM244Z0tCWVhCeDMySHI5djBEWTBZK1RjCklvUEhBWHVUQWdNQkFBRUNnZ0VBYVNJVjh1ZmJ0Nit3elpUV1VYTWZNSnBkaXVHQmtJenBQdG9RbVJnbENNOXIKSExmRXFLVGZUajA1WkRjWENpMlM4cTVGWW5lYTlHQWg0UXN2dlMwaEZkSldoU3BHZVFTREhVV05YUXIzWVVwOQpoT1ZiU0tNMzl3eEU2YXMreWE5NnZmVmRBc0JZakhSNjcwbVNlN25reUpiOVFtaERzcENkUXZJbmVNWjJWaEowClNBRHA4TFMwVFprbElDNy9xOGhDaERYVHNUd3oraUMzQVF0YWdUWHlqeURlSUhSdE1JRENJbzYwWnhpak5uTUwKSUlRMU9EUWNETGZidThTOU9hTlFRbUVwYXRmZXlJVGUxS3NIN1JIMEZzZEZKd3oxc2lOOUNUcmduZzl0YlZ0NAoyWlRoQjhwRVlLQzByamhzVi9WNTRrQVdUdUR5dFF1dURkZUZMMUlSc1FLQmdRREdpdnkwdGtZcTlvTnFIKzBoClZnd0dlMzU2OGtMUGhyMld1bS9aNVl0L3ZDbW9iVEorUURpMHFIRmp6U2lxZE1hd09EV25vOUNvWTVMMEFraXUKekx4RU1nV05LSjJoMk9mZ21lcGJMc2MwS29SKzhwSWtIVjFkMlFsRlhFaDcvdFNXRWczQ3paODdLNUxsUVdwOAptMWMydEJmYjV0ZzJUaWtDeldhbTN5Z21Dd0tCZ1FDOE02bUdXVGNyY3BYN1BwT2xkaW52QkpWL2dSa0pJa2plClQzSmR1L3M1QkoxZGRMYmFJcVFITEdmeHlnQ3ZsdEJjOVVxZ2tmVjltaWpYUGR5bjUySHZ6ck5rTWNLTUlYbksKRXNqdXlmN0pWQXlGR284MjRTSzlVdi9kb2krdkhERktHT2hSR2RkVUhpajB6bEdaaTlFS0tFdFFyUnQxWEFZMQpHQlNLek1xZG1RS0JnRjcxZ05JcHo3dWl0YzNmUVRwTmZtam5UZUlkMXFpTktFWmNHejBiVmJJZFc4dExsdmZkClhZSENncUVhTWRYOURqNmdVOEUvVDRBS1IvSGUyY2FJODQ2bVloTldscllmR1NCS1hWV0pOUXVWUGxPOVR2cG8KOVF2Nnp6MVdXdkM3UEJBZXlHZ2drb1RwWFZPN0N1UllJOGx0TDNBa284cXRiVjRDd1pNVWJvNXJBb0dBRUZ0dApJSUFnaTJZcUl1d05hUlFjRU9vVkZEL2tMU2NOcmtTNWErd3FxWW45STJKQmdqUUFqWjhPYWJWazJkNEJ2aEtUCnFlRUZ0U1R3NThRNHFWOHk0K1RUTXFJZ2hvMWlTRzNaaU9lRmZYb1FuSUR5Y1pLZnNsVEVhRDd3WmdmTlFPTnAKVGFNeFU5NUxNUHU5bTlyTEVGYndOTkxXMEJ4dmJhM3FHeVl4ZGdFQ2dZRUF1ZEdSU1hYa3lrSzl3b1FYd1l5NQpRNjdwKzZkRzBWMCtMMWJsTjg5UDJWMnp0ZGFQby95a3dVZUQwd1BpV3pEUkZBY3V4ampYTlNqRHN1Nndid2hKCmlISFBZMFJYbVhCZ3RlOVREb2lNZ3FnNzQ1b3lZamJxa3ZiV0JzK0dOT3kzcDQ5cHRpUUNLajFCQUQ0L0F3MkkKQ0g3SGdQMS9STm02TTVZMVQwbFNBY289Ci0tLS0tRU5EIFBSSVZBVEUgS0VZLS0tLS0=",
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

    public static FiksIOMessageResponse GetFiksIOMessageResponse(
        string messageType = FiksArkivMeldingtype.ArkivmeldingOpprettMottatt,
        Guid? inReplyToMessage = null,
        string? correlationId = null
    )
    {
        return new FiksIOMessageResponse(
            SendtMelding.FromSentMessageApiModel(
                new SendtMeldingApiModel
                {
                    MeldingId = Guid.NewGuid(),
                    MeldingType = messageType,
                    AvsenderKontoId = Guid.NewGuid(),
                    MottakerKontoId = Guid.NewGuid(),
                    SvarPaMelding = inReplyToMessage,
                    Headere = new Dictionary<string, string>
                    {
                        [MeldingBase.HeaderKlientKorrelasjonsId] = correlationId ?? string.Empty,
                    },
                }
            )
        );
    }

    public static FiksIOMessageRequest GetFiksIOMessageRequest(
        string messageType = FiksArkivMeldingtype.ArkivmeldingOpprett,
        Guid? recipient = null,
        Guid? sendersReference = null,
        Guid? inReplyToMessage = null,
        string? correlationId = null,
        IEnumerable<FiksIOMessagePayload>? payload = null
    )
    {
        return new FiksIOMessageRequest(
            recipient ?? Guid.NewGuid(),
            messageType,
            sendersReference ?? Guid.NewGuid(),
            payload ?? [],
            inReplyToMessage,
            correlationId
        );
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

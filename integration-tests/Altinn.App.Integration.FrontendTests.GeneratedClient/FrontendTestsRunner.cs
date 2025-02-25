using System.Text.Json;
using Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated;
using Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Models;
using Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item;
using Altinn.App.Integration.FrontendTests.GeneratedClient.KiotaGenerated.Ttd.FrontendTest.Instances.Item.Item.Data;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Bundle;

namespace Altinn.App.Integration.FrontendTests.GeneratedClient;

public class FrontendTestsRunner
{
    private readonly HttpClient _client;
    private readonly Action<string> _logger;

    public FrontendTestsRunner(HttpClient client, Action<string> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task RunMultipleSteps()
    {
        var userId = 1337;
        var partyId = 501337;
        var apiClient = new FrontendTestsClient(
            new DefaultRequestAdapter(new LocaltestAuthProvider(userId, _client, _logger), httpClient: _client)
        );

        var instance = await apiClient.Ttd.FrontendTest.Instances.PostAsync(
            new InstanceWrite() { InstanceOwner = new() { PartyId = $"{partyId}" } }
        );
        Console.WriteLine(JsonSerializer.Serialize(instance));
        if (instance?.Id is null)
        {
            throw new InvalidOperationException("Instance is null");
        }

        var dataId = instance.Data?.FirstOrDefault(d => d.DataType == "message")?.Id;
        if (dataId is null)
        {
            throw new InvalidOperationException("No data element of type 'message' found");
        }

        var instanceId = instance.Id.Split('/')[1];
        var data = await apiClient.Ttd.FrontendTest.Instances[partyId][instanceId].Data[dataId].Type.Message.GetAsync();

        var body = data?.Body;

        try
        {
            var patchResponseError = await apiClient
                .Ttd.FrontendTest.Instances[partyId][instanceId]
                .Data.PatchAsync(
                    new DataPatchRequestBody()
                    {
                        Patches =
                        [
                            new()
                            {
                                DataElementId = dataId,
                                Patch =
                                [
                                    new()
                                    {
                                        Op = DataPatchRequestBody_patches_patch_op.Test,
                                        Path = "/Body",
                                        Value = new UntypedString(body + "error"),
                                    },
                                ],
                            },
                        ],
                    }
                );
        }
        catch (DataPatchError e)
        {
            _logger(e.Title ?? "No title in error");
            _logger(e.Detail ?? "No detail in error");

            if (e.Title != "Precondition in patch failed")
                throw new Exception(
                    "Unexpected error in title, expected 'Precondition in patch failed', was " + e.Title
                );
        }

        var patchResponse = await apiClient
            .Ttd.FrontendTest.Instances[partyId][instanceId]
            .Data.PatchAsync(
                new()
                {
                    Patches =
                    [
                        new()
                        {
                            DataElementId = dataId,
                            Patch =
                            [
                                new()
                                {
                                    Op = DataPatchRequestBody_patches_patch_op.Test,
                                    Path = "/Body",
                                    Value = body is null ? new UntypedNull() : new UntypedString(body),
                                },
                                new()
                                {
                                    Op = DataPatchRequestBody_patches_patch_op.Replace,
                                    Path = "/Body",
                                    Value = new UntypedString("test"),
                                },
                            ],
                        },
                    ],
                }
            );

        var pathedData = await apiClient
            .Ttd.FrontendTest.Instances[partyId][instanceId]
            .Data[dataId]
            .Type.Message.GetAsync();
        if (pathedData?.Body != "test")
        {
            throw new Exception("Data not updated. Expected /Body to be 'test', was " + pathedData?.Body);
        }

        pathedData.Title = "New title";

        await apiClient.Ttd.FrontendTest.Instances[partyId][instanceId].Data[dataId].Type.Message.PutAsync(pathedData);

        var updatedDataViaPut = await apiClient
            .Ttd.FrontendTest.Instances[partyId][instanceId]
            .Data[dataId]
            .Type.Message.GetAsync();
        if (updatedDataViaPut?.Title != "New title")
        {
            throw new Exception("Data not updated. Expected /Title to be 'New title', was " + updatedDataViaPut?.Title);
        }

        var nextResult = await apiClient
            .Ttd.FrontendTest.Instances[partyId][instanceId]
            .Process.Next.PutAsync(
                new ProcessNext()
                {
                    // Action = "next"
                }
            );
        throw new Exception("Expected 409 error, got " + nextResult);
    }
}

public class LocaltestAuthProvider(int userId, HttpClient client, Action<string> logger) : IAuthenticationProvider
{
    private Lazy<Task<string>> _accessToken = new(async () =>
    {
        logger("Getting test user token");
        var response = await client.GetAsync("/Home/GetTestUserToken?userId=" + userId);
        logger("Got test user token: " + response.StatusCode);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadAsStringAsync();
        logger("Token: " + token);
        return token;
    });

    public async Task AuthenticateRequestAsync(
        RequestInformation request,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = new CancellationToken()
    )
    {
        var token = await _accessToken.Value;
        request.Headers.Add("Authorization", $"Bearer {token}");
    }
}

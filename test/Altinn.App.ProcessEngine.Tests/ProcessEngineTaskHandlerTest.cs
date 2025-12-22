using Altinn.App.ProcessEngine;
using Altinn.App.ProcessEngine.Constants;
using Altinn.App.ProcessEngine.Models;
using Xunit;

namespace Altinn.App.ProcessEngine.Tests;

public class ProcessEngineTaskHandlerTest
{
    [Fact]
    public async Task GetAuthorizedAppClient_CreatesClientWithCorrectProperties()
    {
        // Arrange
        await using var fixture = TestFixture.Create();
        var instanceInformation = new InstanceInformation
        {
            App = "test-app",
            Org = "test-org",
            InstanceOwnerPartyId = 1234,
            InstanceGuid = Guid.Parse("d393feb8-fa91-4719-88d9-dc041ee4d6b1"),
        };

        // Act
        var client = fixture.ProcessEngineTaskHandler.GetAuthorizedAppClient(instanceInformation);

        // Assert
        Assert.Equal(
            "http://local.altinn.cloud/test-org/test-app/instances/1234/d393feb8-fa91-4719-88d9-dc041ee4d6b1/process-engine-callbacks/",
            client.BaseAddress!.ToString()
        );
        Assert.True(client.BaseAddress.IsAbsoluteUri);
        Assert.Equal(Defaults.ApiKey, client.DefaultRequestHeaders.GetValues(AuthConstants.ApiKeyHeaderName).Single());
    }
}

using System.Text.Json;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Models;
using FluentAssertions;

namespace Altinn.App.Core.Tests.Features.Correspondence.Models;

public class CorrespondenceResponseTests
{
    [Fact]
    public void ValidResponse_DeserializesCorrectly()
    {
        // Arrange
        var encodedResponse = """
            {
               "correspondences": [
                  {
                     "correspondenceId": "d22d8dda-7b56-48c0-b287-5052aa255d5b",
                     "status": "Initialized",
                     "recipient": "0192:213872702",
                     "notifications": [
                        {
                           "orderId": "0ee29355-f2ca-4cd9-98e0-e97a4242d321",
                           "isReminder": false,
                           "status": "Success"
                        }
                     ]
                  }
               ],
               "attachmentIds": [
                  "cae24499-a5f9-425b-9c5b-4dac85fce891"
               ]
            }
            """;

        // Act
        var parsedResponse = JsonSerializer.Deserialize<CorrespondenceResponse>(encodedResponse);

        // Assert
        Assert.NotNull(parsedResponse);
        Assert.NotNull(parsedResponse.Correspondences);
        Assert.NotNull(parsedResponse.AttachmentIds);

        parsedResponse.Correspondences.Should().HaveCount(1);
        parsedResponse
            .Correspondences[0]
            .CorrespondenceId.Should()
            .Be(Guid.Parse("d22d8dda-7b56-48c0-b287-5052aa255d5b"));
        parsedResponse.Correspondences[0].Status.Should().Be(CorrespondenceStatus.Initialized);
        parsedResponse.Correspondences[0].Recipient.Should().Be(OrganisationNumber.Parse("0192:213872702"));

        parsedResponse.Correspondences[0].Notifications.Should().HaveCount(1);
        parsedResponse
            .Correspondences[0]
            .Notifications![0]
            .OrderId.Should()
            .Be(Guid.Parse("0ee29355-f2ca-4cd9-98e0-e97a4242d321"));
        parsedResponse.Correspondences[0].Notifications![0].IsReminder.Should().BeFalse();
        parsedResponse
            .Correspondences[0]
            .Notifications![0]
            .Status.Should()
            .Be(CorrespondenceNotificationStatus.Success);

        parsedResponse.AttachmentIds.Should().HaveCount(1);
        parsedResponse.AttachmentIds[0].Should().Be(Guid.Parse("cae24499-a5f9-425b-9c5b-4dac85fce891"));
    }
}

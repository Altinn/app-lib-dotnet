namespace Altinn.App.Core.Tests.Internal.AccessManagement.Builders;

using Altinn.App.Core.Internal.AccessManagement.Builders;
using Altinn.App.Core.Internal.AccessManagement.Models;
using Altinn.App.Core.Internal.AccessManagement.Models.Shared;
using Altinn.App.Core.Models;
using FluentAssertions;

public class DelegationBuilderTests
{
    [Fact]
    public void ShouldBuildValidDelegationRequest()
    {
        // Arrange
        var testAppId = "testOrg/testApp";
        AppIdentifier appIdentifier = new(testAppId);
        AppResourceId appResourceId = AppResourceId.FromAppIdentifier(appIdentifier);
        string instanceId = "61c2fe1d-7ff7-4009-9e96-506c56ea3d5e";
        string instanceOwnerPartyId = "50000000";
        string delegateeId = "50000001";
        string taskId = "Task_1";

        var builder = DelegationBuilder
            .Create()
            .WithApplicationId(appIdentifier)
            .WithInstanceId(instanceId)
            .WithDelegator(new Delegator { IdType = DelegationConst.Party, Id = instanceOwnerPartyId })
            .WithDelegatee(new Delegatee { IdType = DelegationConst.Party, Id = delegateeId })
            .WithRights(
                [
                    AccessRightBuilder
                        .Create()
                        .WithAction(ActionType.Read)
                        .WithResources(
                            [
                                new Resource { Value = appResourceId.Value },
                                new Resource { Type = DelegationConst.Task, Value = taskId },
                            ]
                        )
                        .Build(),
                ]
            );

        var expected = new DelegationRequest
        {
            From = new Delegator { IdType = "urn:altinn:party:uuid", Id = "50000000" },
            To = new Delegatee { IdType = "urn:altinn:party:uuid", Id = "50000001" },
            ResourceId = "app_testOrg_testApp",
            InstanceId = "61c2fe1d-7ff7-4009-9e96-506c56ea3d5e",
            Rights =
            [
                new RightRequest
                {
                    Resource =
                    [
                        new Resource { Value = "app_testOrg_testApp" },
                        new Resource { Type = "urn:altinn:task", Value = "Task_1" },
                    ],
                    Action = new AltinnAction
                    {
                        Type = "urn:oasis:names:tc:xacml:1.0:action:action-id",
                        Value = "read",
                    },
                },
            ],
        };

        // Act
        var actual = builder.Build();

        // Assert
        // Compare top-level properties
        actual.From!.IdType.Should().Be(expected.From.IdType);
        actual.From.Id.Should().Be(expected.From.Id);

        actual.To!.IdType.Should().Be(expected.To.IdType);
        actual.To.Id.Should().Be(expected.To.Id);

        actual.ResourceId.Should().Be(expected.ResourceId);
        actual.InstanceId.Should().Be(expected.InstanceId);

        // Compare the Rights collection
        actual
            .Rights.Should()
            .HaveCount(expected.Rights.Count, "they should contain the same number of right requests");

        for (int i = 0; i < actual.Rights.Count; i++)
        {
            var requestRight = actual.Rights[i];
            var expectedRight = expected.Rights[i];

            // Compare the Action
            requestRight.Action!.Type.Should().Be(expectedRight.Action!.Type);
            requestRight.Action.Value.Should().Be(expectedRight.Action.Value);

            // Compare the Resources
            requestRight
                .Resource.Should()
                .HaveCount(expectedRight.Resource.Count, "they should reference the same number of resources");

            for (int j = 0; j < requestRight.Resource.Count; j++)
            {
                var requestRes = requestRight.Resource[j];
                var expectedRes = expectedRight.Resource[j];

                requestRes.Type.Should().Be(expectedRes.Type);
                requestRes.Value.Should().Be(expectedRes.Value);
            }
        }
    }
}

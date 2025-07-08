using System.Security.Claims;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Data;

public class DataElementAccessCheckerTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetReaderProblem_HandlesEmptyRequiredAction(string? actionRequiredToRead)
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = fixture.Data.DefaultInstance;
        var dataType = fixture.Data.DataTypeA;
        var dataElement = fixture.Data.DataElementA;
        dataType.ActionRequiredToRead = actionRequiredToRead;

        // Act
        var result1 = await fixture.DataElementAccessChecker.GetReaderProblem(instance, dataType);
        var result2 = await fixture.DataElementAccessChecker.GetReaderProblem(instance, dataElement);
        var canRead = await fixture.DataElementAccessChecker.CanRead(instance, dataType);

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.True(canRead);
    }

    [Fact]
    public async Task GetReaderProblem_HandlesAuthorizedScenario()
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = fixture.Data.DefaultInstance;
        var dataType = fixture.Data.DataTypeA;
        var dataElement = fixture.Data.DataElementA;
        dataType.ActionRequiredToRead = "specificAuthorizedAction";

        fixture
            .Mocks.AuthorizationServiceMock.Setup(x =>
                x.AuthorizeAction(
                    It.IsAny<AppIdentifier>(),
                    It.IsAny<InstanceIdentifier>(),
                    It.IsAny<ClaimsPrincipal>(),
                    "specificAuthorizedAction",
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(true);

        // Act
        var result1 = await fixture.DataElementAccessChecker.GetReaderProblem(instance, dataType);
        var result2 = await fixture.DataElementAccessChecker.GetReaderProblem(instance, dataElement);
        var canRead = await fixture.DataElementAccessChecker.CanRead(instance, dataType);

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
        Assert.True(canRead);
    }

    [Fact]
    public async Task GetReaderProblem_HandlesUnauthorizedScenario()
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = fixture.Data.DefaultInstance;
        var dataType = fixture.Data.DataTypeA;
        var dataElement = fixture.Data.DataElementA;
        dataType.ActionRequiredToRead = "specificAuthorizedAction";

        // Act
        var result1 = await fixture.DataElementAccessChecker.GetReaderProblem(instance, dataType);
        var result2 = await fixture.DataElementAccessChecker.GetReaderProblem(instance, dataElement);
        var canRead = await fixture.DataElementAccessChecker.CanRead(instance, dataType);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.False(canRead);

        Assert.Equal(result1.Status, result2.Status);
        Assert.Equal(result1.Detail, result2.Detail);
        Assert.Equal(StatusCodes.Status403Forbidden, result1.Status!.Value);
        Assert.Contains("Access denied", result1.Detail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Mutators_HandleEmptyRequiredAction(string? actionRequiredToRead)
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = fixture.Data.DefaultInstance;
        var dataType = fixture.Data.DataTypeA;
        dataType.ActionRequiredToRead = actionRequiredToRead;

        // Act
        var createResult = await fixture.DataElementAccessChecker.GetCreateProblem(instance, dataType);
        var updateResult = await fixture.DataElementAccessChecker.GetUpdateProblem(instance, dataType);
        var deleteResult = await fixture.DataElementAccessChecker.GetDeleteProblem(instance, dataType, Guid.Empty);
        var canCreate = await fixture.DataElementAccessChecker.CanCreate(instance, dataType);
        var canUpdate = await fixture.DataElementAccessChecker.CanUpdate(instance, dataType);
        var canDelete = await fixture.DataElementAccessChecker.CanDelete(instance, dataType, Guid.Empty);

        // Assert
        Assert.Null(createResult);
        Assert.Null(updateResult);
        Assert.Null(deleteResult);
        Assert.True(canCreate);
        Assert.True(canUpdate);
        Assert.True(canDelete);
    }

    [Fact]
    public async Task Mutators_EnforceActiveInstance()
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = fixture.Data.DefaultInstance;
        var dataType = fixture.Data.DataTypeA;
        instance.Status = new InstanceStatus { IsArchived = true, Archived = DateTime.Now };

        // Act
        var createResult = await fixture.DataElementAccessChecker.GetCreateProblem(instance, dataType);
        var updateResult = await fixture.DataElementAccessChecker.GetUpdateProblem(instance, dataType);
        var deleteResult = await fixture.DataElementAccessChecker.GetDeleteProblem(instance, dataType, Guid.Empty);
        var canCreate = await fixture.DataElementAccessChecker.CanCreate(instance, dataType);
        var canUpdate = await fixture.DataElementAccessChecker.CanUpdate(instance, dataType);
        var canDelete = await fixture.DataElementAccessChecker.CanDelete(instance, dataType, Guid.Empty);

        // Assert
        Assert.NotNull(createResult);
        Assert.NotNull(updateResult);
        Assert.NotNull(deleteResult);

        Assert.Equal(createResult.Status, updateResult.Status);
        Assert.Equal(createResult.Status, deleteResult.Status);
        Assert.Equal(StatusCodes.Status409Conflict, createResult.Status!.Value);
        Assert.Contains("archived or deleted instance", createResult.Detail);

        Assert.False(canCreate);
        Assert.False(canUpdate);
        Assert.False(canDelete);
    }

    [Theory]
    [InlineData("invalidOrg", false)]
    [InlineData("validOrg", true)]
    public async Task Mutators_EnforceAllowedContributors(string authOrg, bool expectSuccess)
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = fixture.Data.DefaultInstance;
        var dataType = fixture.Data.DataTypeA;
        var auth = TestAuthentication.GetServiceOwnerAuthentication(org: authOrg);

        dataType.AllowedContributors = ["org:validOrg"];
        fixture.Mocks.AuthenticationContextMock.Setup(x => x.Current).Returns(auth);

        // Act
        var createResult = await fixture.DataElementAccessChecker.GetCreateProblem(instance, dataType, auth);
        var updateResult = await fixture.DataElementAccessChecker.GetUpdateProblem(instance, dataType, auth);
        var deleteResult = await fixture.DataElementAccessChecker.GetDeleteProblem(
            instance,
            dataType,
            Guid.Empty,
            auth
        );
        var canCreate = await fixture.DataElementAccessChecker.CanCreate(instance, dataType);
        var canUpdate = await fixture.DataElementAccessChecker.CanUpdate(instance, dataType);
        var canDelete = await fixture.DataElementAccessChecker.CanDelete(instance, dataType, Guid.Empty);

        // Assert
        Assert.Equal(expectSuccess, canCreate);
        Assert.Equal(expectSuccess, canUpdate);
        Assert.Equal(expectSuccess, canDelete);

        if (expectSuccess)
        {
            Assert.Null(createResult);
            Assert.Null(updateResult);
            Assert.Null(deleteResult);
        }
        else
        {
            Assert.NotNull(createResult);
            Assert.NotNull(updateResult);
            Assert.NotNull(deleteResult);

            Assert.Equal(createResult.Status, updateResult.Status);
            Assert.Equal(createResult.Status, deleteResult.Status);
            Assert.Equal("Forbidden", createResult.Title); // Doesn't have status code for some reason
            Assert.Contains("not a valid contributor", createResult.Detail);
        }
    }

    [Fact]
    public async Task GetCreateProblem_VerifiesMaxCount()
    {
        // Arrange
        var fixture = Fixture.Create();
        var instance = fixture.Data.DefaultInstance;
        var dataType = fixture.Data.DataTypeA;

        dataType.MaxCount = 1;

        // Act
        var createResult = await fixture.DataElementAccessChecker.GetCreateProblem(instance, dataType);
        var canCreate = await fixture.DataElementAccessChecker.CanCreate(instance, dataType);

        // Assert
        Assert.NotNull(createResult);

        Assert.Equal(StatusCodes.Status409Conflict, createResult.Status!.Value);
        Assert.Equal("Max Count Exceeded", createResult.Title);

        Assert.False(canCreate);
    }

    [Fact]
    public async Task GetCreateProblem_VerifiesMaxSize()
    {
        // TODO
    }

    [Fact]
    public async Task GetCreateProblem_EnforcesDisallowUserCreate()
    {
        // TODO
    }

    [Fact]
    public async Task GetDeleteProblem_EnforcesDisallowUserDelete()
    {
        // TODO
    }

    private sealed record Fixture
    {
        public required DataElementAccessChecker DataElementAccessChecker { get; init; }
        public required FixtureMocks Mocks { get; init; }
        public required FixtureData Data { get; init; }

        public static Fixture Create()
        {
            var data = new FixtureData();
            var mocks = new FixtureMocks();

            mocks.AppMetadataMock.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(data.AppMetadata);
            mocks.HttpContextAccessorMock.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
            mocks.AuthenticationContextMock.Setup(x => x.Current).Returns((Authenticated)null!);
            mocks
                .AuthorizationServiceMock.Setup(x =>
                    x.AuthorizeAction(
                        It.IsAny<AppIdentifier>(),
                        It.IsAny<InstanceIdentifier>(),
                        It.IsAny<ClaimsPrincipal>(),
                        It.IsAny<string>(),
                        It.IsAny<string>()
                    )
                )
                .ReturnsAsync(false);

            return new Fixture
            {
                Mocks = mocks,
                Data = data,
                DataElementAccessChecker = new DataElementAccessChecker(
                    mocks.AuthorizationServiceMock.Object,
                    mocks.HttpContextAccessorMock.Object,
                    mocks.AuthenticationContextMock.Object,
                    mocks.AppMetadataMock.Object
                ),
            };
        }

        public sealed record FixtureMocks
        {
            public Mock<IAuthorizationService> AuthorizationServiceMock { get; init; } = new(MockBehavior.Strict);
            public Mock<IHttpContextAccessor> HttpContextAccessorMock { get; init; } = new(MockBehavior.Strict);
            public Mock<IAuthenticationContext> AuthenticationContextMock { get; init; } = new(MockBehavior.Strict);
            public Mock<IAppMetadata> AppMetadataMock { get; init; } = new(MockBehavior.Strict);
        }

        public sealed record FixtureData
        {
            public Instance DefaultInstance { get; }
            public DataType DataTypeA { get; }
            public DataType DataTypeB { get; }
            public DataElement DataElementA { get; }
            public DataElement DataElementB { get; }
            public ApplicationMetadata AppMetadata { get; }

            public FixtureData()
            {
                DataTypeA = new DataType { Id = "test-a" };
                DataTypeB = new DataType { Id = "test-b" };
                DataElementA = new DataElement { Id = "498a4be1-b72c-4895-8023-498b437c00e7", DataType = DataTypeA.Id };
                DataElementB = new DataElement { Id = "2bd70b0c-57ee-4e4c-b75f-8ca4b5acd8be", DataType = DataTypeB.Id };
                AppMetadata = new ApplicationMetadata("app/org") { DataTypes = [DataTypeA, DataTypeB] };

                DefaultInstance = new Instance
                {
                    AppId = "app/org",
                    Id = "501337/b5eb6c95-e79a-4e21-93ca-7acd810f7e41",
                    Data = [DataElementA, DataElementB],
                };
            }
        }
    }
}

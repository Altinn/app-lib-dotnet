using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.Process;

public class ProcessEngineAuthorizerTests
{
    private readonly Mock<IAuthorizationService> _authServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly ProcessEngineAuthorizer _authorizer;
    private readonly ClaimsPrincipal _user;

    public ProcessEngineAuthorizerTests()
    {
        _authServiceMock = new Mock<IAuthorizationService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var loggerMock = new Mock<ILogger<ProcessEngineAuthorizer>>();

        _user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim> { new Claim("sub", "12345"), new Claim("name", "Test User") },
                "TestAuthentication"
            )
        );

        HttpContext httpContext = new DefaultHttpContext { };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        _authorizer = new ProcessEngineAuthorizer(
            _authServiceMock.Object,
            _httpContextAccessorMock.Object,
            loggerMock.Object
        );
    }

    [Fact]
    public async Task AuthorizeProcessNext_WithNullCurrentTask_ReturnsFalse()
    {
        // Arrange
        Instance instance = CreateInstance(null);

        // Act
        bool result = await _authorizer.AuthorizeProcessNext(instance);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeProcessNext_WithSpecificAction_CallsAuthorizationService()
    {
        // Arrange
        Instance instance = CreateInstance("task1", "data");
        const string action = "write";

        _authServiceMock
            .Setup(x =>
                x.AuthorizeAction(
                    It.IsAny<AppIdentifier>(),
                    It.IsAny<InstanceIdentifier>(),
                    It.IsAny<ClaimsPrincipal>(),
                    action,
                    "task1"
                )
            )
            .ReturnsAsync(true);

        // Act
        bool result = await _authorizer.AuthorizeProcessNext(instance, action);

        // Assert
        Assert.True(result);
        _authServiceMock.Verify(
            x =>
                x.AuthorizeAction(
                    It.Is<AppIdentifier>(a => a.Org == "org" && a.App == "app"),
                    It.IsAny<InstanceIdentifier>(),
                    _user,
                    action,
                    "task1"
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AuthorizeProcessNext_WithNoAction_DataTask_ChecksWriteAction()
    {
        // Arrange
        Instance instance = CreateInstance("task1", "data");
        var authorizeActionsResult = new List<UserAction>
        {
            new() { Id = "write", Authorized = true },
        };

        _authServiceMock
            .Setup(x =>
                x.AuthorizeActions(
                    It.IsAny<Instance>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.Is<List<AltinnAction>>(a => a.Any(action => action.Value == "write"))
                )
            )
            .ReturnsAsync(authorizeActionsResult);

        // Act
        bool result = await _authorizer.AuthorizeProcessNext(instance);

        // Assert
        Assert.True(result);
        _authServiceMock.Verify(
            x =>
                x.AuthorizeActions(
                    instance,
                    _user,
                    It.Is<List<AltinnAction>>(a => a.Count == 1 && a[0].Value == "write")
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AuthorizeProcessNext_WithNoAction_PaymentTask_ChecksBothPayAndWriteActions()
    {
        // Arrange
        Instance instance = CreateInstance("task1", "payment");
        var authorizeActionsResult = new List<UserAction>
        {
            new() { Id = "pay", Authorized = false },
            new() { Id = "write", Authorized = true },
        };

        _authServiceMock
            .Setup(x =>
                x.AuthorizeActions(
                    It.IsAny<Instance>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.Is<List<AltinnAction>>(a =>
                        a.Any(action => action.Value == "pay") && a.Any(action => action.Value == "write")
                    )
                )
            )
            .ReturnsAsync(authorizeActionsResult);

        // Act
        bool result = await _authorizer.AuthorizeProcessNext(instance);

        // Assert
        Assert.True(result);
        _authServiceMock.Verify(
            x =>
                x.AuthorizeActions(
                    instance,
                    _user,
                    It.Is<List<AltinnAction>>(a =>
                        a.Count == 2 && a.Any(act => act.Value == "pay") && a.Any(act => act.Value == "write")
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AuthorizeProcessNext_WithNoAction_ConfirmationTask_ChecksConfirmAction()
    {
        // Arrange
        Instance instance = CreateInstance("task1", "confirmation");
        var authorizeActionsResult = new List<UserAction>
        {
            new() { Id = "confirm", Authorized = true },
        };

        _authServiceMock
            .Setup(x =>
                x.AuthorizeActions(
                    It.IsAny<Instance>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.Is<List<AltinnAction>>(a => a.Any(action => action.Value == "confirm"))
                )
            )
            .ReturnsAsync(authorizeActionsResult);

        // Act
        bool result = await _authorizer.AuthorizeProcessNext(instance);

        // Assert
        Assert.True(result);
        _authServiceMock.Verify(
            x =>
                x.AuthorizeActions(
                    instance,
                    _user,
                    It.Is<List<AltinnAction>>(a => a.Count == 1 && a[0].Value == "confirm")
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AuthorizeProcessNext_WithNoAction_SigningTask_ChecksSignAndWriteActions()
    {
        // Arrange
        Instance instance = CreateInstance("task1", "signing");
        var authorizeActionsResult = new List<UserAction>
        {
            new() { Id = "sign", Authorized = false },
            new() { Id = "write", Authorized = true },
        };

        _authServiceMock
            .Setup(x =>
                x.AuthorizeActions(
                    It.IsAny<Instance>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.Is<List<AltinnAction>>(a =>
                        a.Any(action => action.Value == "sign") && a.Any(action => action.Value == "write")
                    )
                )
            )
            .ReturnsAsync(authorizeActionsResult);

        // Act
        bool result = await _authorizer.AuthorizeProcessNext(instance);

        // Assert
        Assert.True(result);
        _authServiceMock.Verify(
            x =>
                x.AuthorizeActions(
                    instance,
                    _user,
                    It.Is<List<AltinnAction>>(a =>
                        a.Count == 2 && a.Any(act => act.Value == "sign") && a.Any(act => act.Value == "write")
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task AuthorizeProcessNext_WithNoAuthorizedActions_ReturnsFalse()
    {
        // Arrange
        Instance instance = CreateInstance("task1", "data");
        var authorizeActionsResult = new List<UserAction>
        {
            new() { Id = "write", Authorized = false },
        };

        _authServiceMock
            .Setup(x =>
                x.AuthorizeActions(It.IsAny<Instance>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<List<AltinnAction>>())
            )
            .ReturnsAsync(authorizeActionsResult);

        // Act
        bool result = await _authorizer.AuthorizeProcessNext(instance);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AuthorizeProcessNext_NoHttpContext_ThrowsAuthenticationContextException()
    {
        // Arrange
        Instance instance = CreateInstance("task1", "data");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationContextException>(
            async () => await _authorizer.AuthorizeProcessNext(instance)
        );
    }

    private static Instance CreateInstance(string? taskId, string? taskType = null)
    {
        var instance = new Instance
        {
            Id = "1337/12df57b6-cecf-4e7d-9415-857d93a817b3",
            InstanceOwner = new InstanceOwner { PartyId = "1337" },
            AppId = "app",
            Org = "org",
            Process = new ProcessState(),
        };

        if (taskId != null)
        {
            instance.Process.CurrentTask = new ProcessElementInfo
            {
                ElementId = taskId,
                AltinnTaskType = taskType ?? "unknown",
            };
        }

        return instance;
    }
}

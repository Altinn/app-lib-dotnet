using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using EmailModel = Altinn.App.Core.Features.Signing.Models.Email;
using InternalOrganisationSignee = Altinn.App.Core.Features.Signing.Models.Signee.OrganisationSignee;
using InternalPersonSignee = Altinn.App.Core.Features.Signing.Models.Signee.PersonSignee;
using NotificationModel = Altinn.App.Core.Features.Signing.Models.Notification;
using NotificationsModel = Altinn.App.Core.Features.Signing.Models.Notifications;
using SmsModel = Altinn.App.Core.Features.Signing.Models.Sms;

namespace Altinn.App.Core.Tests.Features.Signing;

public sealed class SigneeContextsManagerTests : IDisposable
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.Preserve,
        MaxDepth = 16,
    };
    private readonly ServiceProvider _serviceProvider;
    private readonly SigneeContextsManager _signeeContextsManager;

    private readonly Mock<IAltinnPartyClient> _altinnPartyClient = new(MockBehavior.Strict);
    private readonly Mock<ISigneeProvider> _signeeProvider = new(MockBehavior.Strict);
    private readonly Mock<ILogger<SigneeContextsManager>> _logger = new();
    private readonly AppImplementationFactory _appImplementationFactory;

    public void Dispose() => _serviceProvider.Dispose();

    public SigneeContextsManagerTests()
    {
        var services = new ServiceCollection();
        services.AddAppImplementationFactory();
        services.AddSingleton(_signeeProvider.Object);
        _serviceProvider = services.BuildServiceProvider();

        _appImplementationFactory = _serviceProvider.GetRequiredService<AppImplementationFactory>();

        _signeeContextsManager = new SigneeContextsManager(
            _altinnPartyClient.Object,
            _appImplementationFactory,
            _logger.Object
        );

        // Setup default party lookup behavior
        _altinnPartyClient
            .Setup(x => x.LookupParty(It.IsAny<PartyLookup>()))
            .ReturnsAsync(
                (PartyLookup lookup) =>
                {
                    if (lookup.Ssn is not null)
                    {
                        return new Party
                        {
                            SSN = lookup.Ssn,
                            Name = "Test Person",
                            Person = new Person
                            {
                                SSN = lookup.Ssn,
                                Name = "Test Person",
                                MobileNumber = "12345678",
                            },
                        };
                    }

                    if (lookup.OrgNo is not null)
                    {
                        return new Party
                        {
                            OrgNumber = lookup.OrgNo,
                            Name = "Test Organization",
                            Organization = new Organization
                            {
                                OrgNumber = lookup.OrgNo,
                                Name = "Test Organization",
                                EMailAddress = "test@org.com",
                                MobileNumber = "87654321",
                            },
                        };
                    }

                    return null!;
                }
            );
    }

    [Fact]
    public async Task GenerateSigneeContexts_WithValidPersonSignees_ReturnsCorrectSigneeContexts()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = "testProvider",
            SigneeStatesDataTypeId = "signeeStates",
        };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        var personSignee1 = new PersonSignee
        {
            SocialSecurityNumber = "12345678901",
            FullName = "Person One",
            Notifications = new NotificationsModel
            {
                OnSignatureAccessRightsDelegated = new NotificationModel
                {
                    Email = new EmailModel { EmailAddress = "person1@example.com" },
                    Sms = new SmsModel { MobileNumber = "11111111" },
                },
            },
        };

        var personSignee2 = new PersonSignee
        {
            SocialSecurityNumber = "10987654321",
            FullName = "Person Two",
            Notifications = new NotificationsModel
            {
                OnSignatureAccessRightsDelegated = new NotificationModel
                {
                    Email = new EmailModel { EmailAddress = "person2@example.com" },
                    Sms = new SmsModel { MobileNumber = "22222222" },
                },
            },
        };

        var signeesResult = new SigneesResult { Signees = [personSignee1, personSignee2] };

        _signeeProvider.Setup(x => x.Id).Returns("testProvider");

        _signeeProvider.Setup(x => x.GetSigneesAsync(It.IsAny<Instance>())).ReturnsAsync(signeesResult);

        // Act
        var result = await _signeeContextsManager.GenerateSigneeContexts(
            cachedInstanceMutator.Object,
            signatureConfiguration,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // Verify first signee context
        var firstContext = result[0];
        Assert.Equal("Task_1", firstContext.TaskId);
        Assert.NotNull(firstContext.SigneeState);
        Assert.False(firstContext.SigneeState.IsAccessDelegated);
        Assert.False(firstContext.SigneeState.HasBeenMessagedForCallToSign);

        Assert.IsType<InternalPersonSignee>(firstContext.Signee);
        var firstSignee = (InternalPersonSignee)firstContext.Signee;
        Assert.Equal("12345678901", firstSignee.SocialSecurityNumber);
        Assert.Equal("Test Person", firstSignee.FullName);

        Assert.NotNull(firstContext.Notifications);
        Assert.NotNull(firstContext.Notifications.OnSignatureAccessRightsDelegated);
        Assert.NotNull(firstContext.Notifications.OnSignatureAccessRightsDelegated.Email);
        Assert.Equal(
            "person1@example.com",
            firstContext.Notifications.OnSignatureAccessRightsDelegated.Email.EmailAddress
        );
        Assert.NotNull(firstContext.Notifications.OnSignatureAccessRightsDelegated.Sms);
        Assert.Equal("11111111", firstContext.Notifications.OnSignatureAccessRightsDelegated.Sms.MobileNumber);

        // Verify second signee context
        var secondContext = result[1];
        Assert.Equal("Task_1", secondContext.TaskId);
        Assert.NotNull(secondContext.SigneeState);
        Assert.False(secondContext.SigneeState.IsAccessDelegated);
        Assert.False(secondContext.SigneeState.HasBeenMessagedForCallToSign);

        Assert.IsType<InternalPersonSignee>(secondContext.Signee);
        var secondSignee = (InternalPersonSignee)secondContext.Signee;
        Assert.Equal("10987654321", secondSignee.SocialSecurityNumber);
        Assert.Equal("Test Person", secondSignee.FullName);

        Assert.NotNull(secondContext.Notifications);
        Assert.NotNull(secondContext.Notifications.OnSignatureAccessRightsDelegated);
        Assert.NotNull(secondContext.Notifications.OnSignatureAccessRightsDelegated.Email);
        Assert.Equal(
            "person2@example.com",
            secondContext.Notifications.OnSignatureAccessRightsDelegated.Email.EmailAddress
        );
        Assert.NotNull(secondContext.Notifications.OnSignatureAccessRightsDelegated.Sms);
        Assert.Equal("22222222", secondContext.Notifications.OnSignatureAccessRightsDelegated.Sms.MobileNumber);
    }

    [Fact]
    public async Task GenerateSigneeContexts_WithValidOrganisationSignees_ReturnsCorrectSigneeContexts()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = "testProvider",
            SigneeStatesDataTypeId = "signeeStates",
        };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        var orgSignee = new OrganisationSignee
        {
            OrganisationNumber = "123456789",
            Name = "Test Organization",
            Notifications = new NotificationsModel
            {
                OnSignatureAccessRightsDelegated = new NotificationModel
                {
                    Email = new EmailModel { }, // Empty to test auto-fill from Party
                    Sms = new SmsModel { }, // Empty to test auto-fill from Party
                },
            },
        };

        var signeesResult = new SigneesResult { Signees = [orgSignee] };

        _signeeProvider.Setup(x => x.Id).Returns("testProvider");

        _signeeProvider.Setup(x => x.GetSigneesAsync(It.IsAny<Instance>())).ReturnsAsync(signeesResult);

        // Act
        var result = await _signeeContextsManager.GenerateSigneeContexts(
            cachedInstanceMutator.Object,
            signatureConfiguration,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var context = result[0];
        Assert.Equal("Task_1", context.TaskId);
        Assert.NotNull(context.SigneeState);
        Assert.False(context.SigneeState.IsAccessDelegated);
        Assert.False(context.SigneeState.HasBeenMessagedForCallToSign);

        Assert.IsType<InternalOrganisationSignee>(context.Signee);
        var signee = (InternalOrganisationSignee)context.Signee;
        Assert.Equal("123456789", signee.OrgNumber);
        Assert.Equal("Test Organization", signee.OrgName);

        Assert.NotNull(context.Notifications);
        Assert.NotNull(context.Notifications.OnSignatureAccessRightsDelegated);
        Assert.NotNull(context.Notifications.OnSignatureAccessRightsDelegated.Email);
        Assert.Equal("test@org.com", context.Notifications.OnSignatureAccessRightsDelegated.Email.EmailAddress);
        Assert.NotNull(context.Notifications.OnSignatureAccessRightsDelegated.Sms);
        Assert.Equal("87654321", context.Notifications.OnSignatureAccessRightsDelegated.Sms.MobileNumber);
    }

    [Fact]
    public async Task GenerateSigneeContexts_WithNoSigneeProvider_ReturnsEmptyList()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = null,
            SigneeStatesDataTypeId = "signeeStates",
        };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        // Act
        var result = await _signeeContextsManager.GenerateSigneeContexts(
            cachedInstanceMutator.Object,
            signatureConfiguration,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateSigneeContexts_WithNoMatchingProvider_ThrowsSigneeProviderNotFoundException()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = "nonExistentProvider",
            SigneeStatesDataTypeId = "signeeStates",
        };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        _signeeProvider.Setup(x => x.Id).Returns("testProvider");

        // Act & Assert
        await Assert.ThrowsAsync<SigneeProviderNotFoundException>(
            () =>
                _signeeContextsManager.GenerateSigneeContexts(
                    cachedInstanceMutator.Object,
                    signatureConfiguration,
                    CancellationToken.None
                )
        );
    }

    [Fact]
    public async Task GetSigneeContexts_WithNoSigneeStatesDataTypeId_ReturnsEmptyList()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = "testProvider",
            SigneeStatesDataTypeId = null,
        };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
        };

        var cachedInstanceAccessor = new Mock<IInstanceDataAccessor>();
        cachedInstanceAccessor.Setup(x => x.Instance).Returns(instance);

        // Act
        var result = await _signeeContextsManager.GetSigneeContexts(
            cachedInstanceAccessor.Object,
            signatureConfiguration
        );

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSigneeContexts_WithNoMatchingDataElement_ReturnsEmptyList()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = "testProvider",
            SigneeStatesDataTypeId = "signeeStates",
        };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
            Data = [],
        };

        var cachedInstanceAccessor = new Mock<IInstanceDataAccessor>();
        cachedInstanceAccessor.Setup(x => x.Instance).Returns(instance);

        // Act
        var result = await _signeeContextsManager.GetSigneeContexts(
            cachedInstanceAccessor.Object,
            signatureConfiguration
        );

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSigneeContexts_WithValidDataElement_ReturnsDeserializedSigneeContexts()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = "testProvider",
            SigneeStatesDataTypeId = "signeeStates",
        };

        var signeeStateDataElement = new DataElement { Id = Guid.NewGuid().ToString(), DataType = "signeeStates" };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
            Data = [signeeStateDataElement],
        };

        // Create test signee contexts to serialize
        var signeeContexts = new List<SigneeContext>
        {
            new()
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState
                {
                    IsAccessDelegated = true,
                    HasBeenMessagedForCallToSign = true,
                    CtaCorrespondenceId = Guid.NewGuid(),
                },
                Signee = new InternalPersonSignee
                {
                    FullName = "Test Person",
                    SocialSecurityNumber = "12345678901",
                    Party = new Party { SSN = "12345678901", Name = "Test Person" },
                },
                Notifications = new NotificationsModel
                {
                    OnSignatureAccessRightsDelegated = new NotificationModel
                    {
                        Email = new EmailModel { EmailAddress = "test@example.com" },
                        Sms = new SmsModel { MobileNumber = "12345678" },
                    },
                },
            },
        };

        // Serialize the signee contexts
        var serializedData = JsonSerializer.SerializeToUtf8Bytes(signeeContexts, _jsonSerializerOptions);

        var cachedInstanceAccessor = new Mock<IInstanceDataAccessor>();
        cachedInstanceAccessor.Setup(x => x.Instance).Returns(instance);
        cachedInstanceAccessor
            .Setup(x => x.GetBinaryData(signeeStateDataElement))
            .ReturnsAsync(new ReadOnlyMemory<byte>(serializedData));

        // Act
        var result = await _signeeContextsManager.GetSigneeContexts(
            cachedInstanceAccessor.Object,
            signatureConfiguration
        );

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var context = result[0];
        Assert.Equal("Task_1", context.TaskId);
        Assert.NotNull(context.SigneeState);
        Assert.True(context.SigneeState.IsAccessDelegated);
        Assert.True(context.SigneeState.HasBeenMessagedForCallToSign);
        Assert.NotNull(context.SigneeState.CtaCorrespondenceId);

        Assert.IsType<InternalPersonSignee>(context.Signee);
        var signee = (InternalPersonSignee)context.Signee;
        Assert.Equal("12345678901", signee.SocialSecurityNumber);
        Assert.Equal("Test Person", signee.FullName);

        Assert.NotNull(context.Notifications);
        Assert.NotNull(context.Notifications.OnSignatureAccessRightsDelegated);
        Assert.NotNull(context.Notifications.OnSignatureAccessRightsDelegated.Email);
        Assert.Equal("test@example.com", context.Notifications.OnSignatureAccessRightsDelegated.Email.EmailAddress);
        Assert.NotNull(context.Notifications.OnSignatureAccessRightsDelegated.Sms);
        Assert.Equal("12345678", context.Notifications.OnSignatureAccessRightsDelegated.Sms.MobileNumber);
    }

    [Fact]
    public async Task GetSigneeContexts_WithMissingSigneeStatesDataTypeId_ThrowsApplicationConfigException()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeProviderId = "testProvider",
            SigneeStatesDataTypeId = null,
        };

        var instance = new Instance
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
        };

        var cachedInstanceAccessor = new Mock<IInstanceDataAccessor>();
        cachedInstanceAccessor.Setup(x => x.Instance).Returns(instance);

        // Act & Assert - This should not throw since the method handles null SigneeStatesDataTypeId
        var result = await _signeeContextsManager.GetSigneeContexts(
            cachedInstanceAccessor.Object,
            signatureConfiguration
        );

        Assert.Empty(result);
    }
}

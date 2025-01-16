﻿using System.Text;
using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Features.Signing;

public class SigningServiceTests
{
    private readonly SigningService _signingService;

    private readonly Mock<IPersonClient> _personClient = new(MockBehavior.Strict);
    private readonly Mock<IOrganizationClient> _organizationClient = new(MockBehavior.Strict);
    private readonly Mock<IAltinnPartyClient> _altinnPartyClient = new(MockBehavior.Strict);
    private readonly Mock<ISigningDelegationService> _signingDelegationService = new(MockBehavior.Strict);
    private readonly Mock<ISigningNotificationService> _signingNotificationService = new(MockBehavior.Strict);
    private readonly Mock<ISigneeProvider> _signeeProvider = new(MockBehavior.Strict);
    private readonly Mock<ILogger<SigningService>> _logger = new(MockBehavior.Strict);

    public SigningServiceTests()
    {
        _signingService = new SigningService(
            _personClient.Object,
            _organizationClient.Object,
            _altinnPartyClient.Object,
            _signingDelegationService.Object,
            _signingNotificationService.Object,
            [_signeeProvider.Object],
            _logger.Object
        );
    }

    [Fact]
    public async Task GetSigneeContexts()
    {
        // Arrange
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeStatesDataTypeId = "signeeStates",
            SignatureDataType = "signature",
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();

        var signeeStateDataElement = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = signatureConfiguration.SigneeStatesDataTypeId,
        };

        var signDocumentDataElement = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = signatureConfiguration.SignatureDataType,
        };

        var signDocumentDataElement2 = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = signatureConfiguration.SignatureDataType,
        };

        Instance instance = new()
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
            Data = [signeeStateDataElement, signDocumentDataElement, signDocumentDataElement2],
        };

        var org = new Organization { OrgNumber = "123456789", Name = "An org" };

        var signeeState = new List<SigneeContext>
        {
            new()
            {
                TaskId = instance.Process.CurrentTask.ElementId,
                SigneeState = new SigneeState { IsAccessDelegated = true },
                OrganisationSignee = new OrganisationSignee
                {
                    DisplayName = org.Name,
                    OrganisationNumber = org.OrgNumber,
                },
                Party = new Party
                {
                    Organization = new Organization { OrgNumber = org.OrgNumber, Name = org.Name },
                },
            },
        };

        var signDocumentWithMatchingSignatureContext = new SignDocument
        {
            SigneeInfo = new Signee { OrganisationNumber = signeeState.First().Party.Organization.OrgNumber },
        };

        var person = new Person { SSN = "12345678910", Name = "A person" };

        var signDocumentWithoutMatchingSignatureContext = new SignDocument
        {
            SigneeInfo = new Signee { PersonNumber = person.SSN },
        };

        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);
        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signeeStateDataElement.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signeeState)));

        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signDocumentDataElement.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signDocumentWithMatchingSignatureContext)));

        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signDocumentDataElement2.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signDocumentWithoutMatchingSignatureContext)));

        _altinnPartyClient
            .Setup(x => x.LookupParty(Match.Create<PartyLookup>(p => p.Ssn == person.SSN)))
            .ReturnsAsync(new Party { Person = person });

        // Act
        List<SigneeContext> result = await _signingService.GetSigneeContexts(
            cachedInstanceMutator.Object,
            signatureConfiguration
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        SigneeContext signeeContextWithMatchingSignatureDocument = result.First(x =>
            x.Party.Organization.OrgNumber == org.OrgNumber
        );

        Assert.NotNull(signeeContextWithMatchingSignatureDocument);
        Assert.Equal(instance.Process.CurrentTask.ElementId, signeeContextWithMatchingSignatureDocument.TaskId);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.OrganisationSignee);
        Assert.Equal(org.Name, signeeContextWithMatchingSignatureDocument.OrganisationSignee?.DisplayName);
        Assert.Equal(org.OrgNumber, signeeContextWithMatchingSignatureDocument.OrganisationSignee?.OrganisationNumber);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.Party);
        Assert.NotNull(signeeContextWithMatchingSignatureDocument.Party.Organization);
        Assert.Equal(org.OrgNumber, signeeContextWithMatchingSignatureDocument.Party.Organization?.OrgNumber);
        Assert.Equal(org.Name, signeeContextWithMatchingSignatureDocument.Party.Organization?.Name);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SigneeState);
        Assert.True(signeeContextWithMatchingSignatureDocument.SigneeState.IsAccessDelegated);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SignDocument);
        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SignDocument?.SigneeInfo);
        Assert.Equal(
            org.OrgNumber,
            signeeContextWithMatchingSignatureDocument.SignDocument?.SigneeInfo?.OrganisationNumber
        );

        SigneeContext signatureWithOnTheFlySigneeContext = result.First(x => x.Party.Person?.SSN == person.SSN);

        Assert.NotNull(signatureWithOnTheFlySigneeContext);
        Assert.Equal(instance.Process.CurrentTask.ElementId, signatureWithOnTheFlySigneeContext.TaskId);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.PersonSignee);
        Assert.Equal(person.Name, signatureWithOnTheFlySigneeContext.PersonSignee?.DisplayName);
        Assert.Equal(person.SSN, signatureWithOnTheFlySigneeContext.PersonSignee?.SocialSecurityNumber);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.Party);
        Assert.NotNull(signatureWithOnTheFlySigneeContext.Party.Person);
        Assert.Equal(person.SSN, signatureWithOnTheFlySigneeContext.Party.Person?.SSN);
        Assert.Equal(person.Name, signatureWithOnTheFlySigneeContext.Party.Person?.Name);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.SigneeState);
        Assert.True(signatureWithOnTheFlySigneeContext.SigneeState.IsAccessDelegated);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.SignDocument);
        Assert.NotNull(signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo);
        Assert.Equal(person.SSN, signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo?.PersonNumber);
    }

    private static byte[] ToBytes<T>(T obj)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
    }
}

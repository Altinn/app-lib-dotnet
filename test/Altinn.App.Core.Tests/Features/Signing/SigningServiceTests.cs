using System.Text;
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
using FluentAssertions;
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
    private readonly Mock<ISigneeProvider> _signeeProvider = new(MockBehavior.Strict);
    private readonly Mock<ILogger<SigningService>> _logger = new(MockBehavior.Strict);

    public SigningServiceTests()
    {
        _signingService = new SigningService(
            _personClient.Object,
            _organizationClient.Object,
            _altinnPartyClient.Object,
            _signingDelegationService.Object,
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
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        SigneeContext signeeContextWithMatchingSignatureDocument = result.First(x =>
            x.Party.Organization.OrgNumber == org.OrgNumber
        );

        signeeContextWithMatchingSignatureDocument.Should().NotBeNull();
        signeeContextWithMatchingSignatureDocument.TaskId.Should().Be(instance.Process.CurrentTask.ElementId);

        signeeContextWithMatchingSignatureDocument.OrganisationSignee.Should().NotBeNull();
        signeeContextWithMatchingSignatureDocument.OrganisationSignee?.DisplayName.Should().Be(org.Name);
        signeeContextWithMatchingSignatureDocument.OrganisationSignee?.OrganisationNumber.Should().Be(org.OrgNumber);

        signeeContextWithMatchingSignatureDocument.Party.Should().NotBeNull();
        signeeContextWithMatchingSignatureDocument.Party.Organization.Should().NotBeNull();
        signeeContextWithMatchingSignatureDocument.Party.Organization?.OrgNumber.Should().Be(org.OrgNumber);
        signeeContextWithMatchingSignatureDocument.Party.Organization?.Name.Should().Be(org.Name);

        signeeContextWithMatchingSignatureDocument.SigneeState.Should().NotBeNull();
        signeeContextWithMatchingSignatureDocument.SigneeState.IsAccessDelegated.Should().BeTrue();

        signeeContextWithMatchingSignatureDocument.SignDocument.Should().NotBeNull();
        signeeContextWithMatchingSignatureDocument.SignDocument?.SigneeInfo.Should().NotBeNull();
        signeeContextWithMatchingSignatureDocument
            .SignDocument?.SigneeInfo?.OrganisationNumber.Should()
            .Be(org.OrgNumber);

        SigneeContext signatureWithOnTheFlySigneeContext = result.First(x => x.Party.Person?.SSN == person.SSN);

        signatureWithOnTheFlySigneeContext.Should().NotBeNull();
        signatureWithOnTheFlySigneeContext.TaskId.Should().Be(instance.Process.CurrentTask.ElementId);

        signatureWithOnTheFlySigneeContext.PersonSignee.Should().NotBeNull();
        signatureWithOnTheFlySigneeContext.PersonSignee?.DisplayName.Should().Be(person.Name);
        signatureWithOnTheFlySigneeContext.PersonSignee?.SocialSecurityNumber.Should().Be(person.SSN);

        signatureWithOnTheFlySigneeContext.Party.Should().NotBeNull();
        signatureWithOnTheFlySigneeContext.Party.Person.Should().NotBeNull();
        signatureWithOnTheFlySigneeContext.Party.Person?.SSN.Should().Be(person.SSN);
        signatureWithOnTheFlySigneeContext.Party.Person?.Name.Should().Be(person.Name);

        signatureWithOnTheFlySigneeContext.SigneeState.Should().NotBeNull();
        signatureWithOnTheFlySigneeContext.SigneeState.IsAccessDelegated.Should().BeTrue();

        signatureWithOnTheFlySigneeContext.SignDocument.Should().NotBeNull();
        signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo.Should().NotBeNull();
        signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo?.PersonNumber.Should().Be(person.SSN);
    }

    private static byte[] ToBytes<T>(T obj)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
    }
}

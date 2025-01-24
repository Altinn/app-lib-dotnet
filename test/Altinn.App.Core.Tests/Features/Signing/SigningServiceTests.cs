using System.Text;
using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Altinn.App.Core.Tests.Features.Signing;

public class SigningServiceTests
{
    private readonly SigningService _signingService;

    private readonly Mock<IAltinnPartyClient> _altinnPartyClient = new(MockBehavior.Strict);
    private readonly Mock<ISigningDelegationService> _signingDelegationService = new(MockBehavior.Strict);
    private readonly Mock<ISigningNotificationService> _signingNotificationService = new(MockBehavior.Strict);
    private readonly Mock<ISigneeProvider> _signeeProvider = new(MockBehavior.Strict);
    private readonly Mock<ILogger<SigningService>> _logger = new();
    private readonly Mock<IAppMetadata> _appMetadata = new(MockBehavior.Strict);
    private readonly Mock<ISignClient> _signClient = new(MockBehavior.Strict);
    private readonly Mock<ISigningCorrespondenceService> _signingCorrespondenceService = new(MockBehavior.Strict);
    private readonly Mock<IDataClient> _dataClient = new(MockBehavior.Strict);

    public SigningServiceTests()
    {
        _signingService = new SigningService(
            _altinnPartyClient.Object,
            _signingDelegationService.Object,
            _signingNotificationService.Object,
            [_signeeProvider.Object],
            _appMetadata.Object,
            _signClient.Object,
            _signingCorrespondenceService.Object,
            _dataClient.Object,
            _logger.Object
        );
    }

    [Fact]
    public async Task GetSigneeContexts_HappyPath()
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

                OriginalParty = new Party
                {
                    OrgNumber = org.OrgNumber,
                    Organization = new Organization { OrgNumber = org.OrgNumber, Name = org.Name },
                },
                OnBehalfOfOrganisation = new SigneeContextOrganisation
                {
                    Name = org.Name,
                    OrganisationNumber = org.OrgNumber,
                },
            },
        };

        var signDocumentWithMatchingSignatureContext = new SignDocument
        {
            SigneeInfo = new Platform.Storage.Interface.Models.Signee
            {
                OrganisationNumber = signeeState.First().OnBehalfOfOrganisation?.OrganisationNumber,
            },
        };

        var person = new Person { SSN = "12345678910", Name = "A person" };

        var signDocumentWithoutMatchingSignatureContext = new SignDocument
        {
            SigneeInfo = new Platform.Storage.Interface.Models.Signee { PersonNumber = person.SSN },
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
            .Setup(x => x.LookupParty(Match.Create<PartyLookup>(p => p.Ssn == person.SSN || p.OrgNo == org.OrgNumber)))
            .ReturnsAsync(
                new Party
                {
                    SSN = person.SSN,
                    Person = person,
                    OrgNumber = org.OrgNumber,
                    Organization = org,
                }
            );

        // Act
        List<SigneeContext> result = await _signingService.GetSigneeContexts(
            cachedInstanceMutator.Object,
            signatureConfiguration
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        SigneeContext signeeContextWithMatchingSignatureDocument = result.First(x =>
            x.OriginalParty.Organization.OrgNumber == org.OrgNumber
        );

        Assert.NotNull(signeeContextWithMatchingSignatureDocument);
        Assert.Equal(instance.Process.CurrentTask.ElementId, signeeContextWithMatchingSignatureDocument.TaskId);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.OriginalParty);
        Assert.Equal(org.Name, signeeContextWithMatchingSignatureDocument.OriginalParty.Organization?.Name);
        Assert.Equal(org.OrgNumber, signeeContextWithMatchingSignatureDocument.OriginalParty.OrgNumber);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.OriginalParty);
        Assert.NotNull(signeeContextWithMatchingSignatureDocument.OriginalParty.Organization);
        Assert.Equal(org.OrgNumber, signeeContextWithMatchingSignatureDocument.OriginalParty.Organization?.OrgNumber);
        Assert.Equal(org.Name, signeeContextWithMatchingSignatureDocument.OriginalParty.Organization?.Name);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SigneeState);
        Assert.True(signeeContextWithMatchingSignatureDocument.SigneeState.IsAccessDelegated);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SignDocument);
        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SignDocument?.SigneeInfo);
        Assert.Equal(
            org.OrgNumber,
            signeeContextWithMatchingSignatureDocument.SignDocument?.SigneeInfo?.OrganisationNumber
        );

        SigneeContext signatureWithOnTheFlySigneeContext = result.First(x => x.OriginalParty.Person?.SSN == person.SSN);

        Assert.NotNull(signatureWithOnTheFlySigneeContext);
        Assert.Equal(instance.Process.CurrentTask.ElementId, signatureWithOnTheFlySigneeContext.TaskId);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.OriginalParty);
        Assert.Equal(person.Name, signatureWithOnTheFlySigneeContext.OriginalParty.Person?.Name);
        Assert.Equal(person.SSN, signatureWithOnTheFlySigneeContext.OriginalParty.SSN);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.OriginalParty);
        Assert.NotNull(signatureWithOnTheFlySigneeContext.OriginalParty.Person);
        Assert.Equal(person.SSN, signatureWithOnTheFlySigneeContext.OriginalParty.Person?.SSN);
        Assert.Equal(person.Name, signatureWithOnTheFlySigneeContext.OriginalParty.Person?.Name);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.SigneeState);
        Assert.True(signatureWithOnTheFlySigneeContext.SigneeState.IsAccessDelegated);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.SignDocument);
        Assert.NotNull(signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo);
        Assert.Equal(person.SSN, signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo?.PersonNumber);
    }

    [Fact]
    public void RemoveSingingData_Removes_SigneeState_And_Signatures()
    {
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeStatesDataTypeId = "signeeStates",
            SignatureDataType = "signature",
        };

        var signeeStateDataElement = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = signatureConfiguration.SigneeStatesDataTypeId,
        };

        var signatureDataElement = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = signatureConfiguration.SignatureDataType,
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        var instance = new Instance { Data = [signeeStateDataElement, signatureDataElement] };

        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        _signingService.RemoveSigningData(cachedInstanceMutator.Object, signatureConfiguration);

        cachedInstanceMutator.Verify(x => x.Instance);
        cachedInstanceMutator.Verify(x => x.RemoveDataElement(signeeStateDataElement), Times.Once);
        cachedInstanceMutator.Verify(x => x.RemoveDataElement(signatureDataElement), Times.Once);
        cachedInstanceMutator.VerifyNoOtherCalls();
    }

    private static byte[] ToBytes<T>(T obj)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
    }
}

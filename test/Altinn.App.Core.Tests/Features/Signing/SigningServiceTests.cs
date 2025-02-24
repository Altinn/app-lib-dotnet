using System.Text;
using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Moq;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using StorageSignee = Altinn.Platform.Storage.Interface.Models.Signee;

namespace Altinn.App.Core.Tests.Features.Signing;

public class SigningServiceTests
{
    private readonly SigningService _signingService;

    private readonly Mock<IAltinnPartyClient> _altinnPartyClient = new(MockBehavior.Strict);
    private readonly Mock<ISigningDelegationService> _signingDelegationService = new(MockBehavior.Strict);
    private readonly Mock<ISigneeProvider> _signeeProvider = new(MockBehavior.Strict);
    private readonly Mock<ILogger<SigningService>> _logger = new();
    private readonly Mock<IAppMetadata> _appMetadata = new(MockBehavior.Strict);
    private readonly Mock<ISignClient> _signClient = new(MockBehavior.Strict);
    private readonly Mock<ISigningCallToActionService> _signingCallToActionService = new(MockBehavior.Strict);
    private readonly Mock<ISigningReceiptService> _signingReceiptService = new(MockBehavior.Strict);

    public SigningServiceTests()
    {
        _signingService = new SigningService(
            _altinnPartyClient.Object,
            _signingDelegationService.Object,
            [_signeeProvider.Object],
            _appMetadata.Object,
            _signClient.Object,
            _signingReceiptService.Object,
            _signingCallToActionService.Object,
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
        var person = new Person { SSN = "12345678910", Name = "A person" };

        List<SigneeContext> signeeContexts =
        [
            new()
            {
                TaskId = instance.Process.CurrentTask.ElementId,
                SigneeState = new SigneeState { IsAccessDelegated = true },

                Signee = new PersonOnBehalfOfOrgSignee
                {
                    FullName = "A person",
                    SocialSecurityNumber = person.SSN,
                    Party = new Party { SSN = person.SSN, Name = person.Name },
                    OnBehalfOfOrg = new OrganisationSignee
                    {
                        OrgName = org.Name,
                        OrgNumber = org.OrgNumber,
                        OrgParty = new Party { Name = org.Name, OrgNumber = org.OrgNumber },
                    },
                },
            },
        ];

        var signDocumentWithMatchingSignatureContext = new SignDocument
        {
            SigneeInfo = new StorageSignee { OrganisationNumber = org.OrgNumber, PersonNumber = person.SSN },
        };

        var signDocumentWithoutMatchingSignatureContext = new SignDocument
        {
            SigneeInfo = new StorageSignee { PersonNumber = person.SSN },
        };

        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);
        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signeeStateDataElement.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signeeContexts)));

        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signDocumentDataElement.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signDocumentWithMatchingSignatureContext)));

        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signDocumentDataElement2.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signDocumentWithoutMatchingSignatureContext)));

        _altinnPartyClient
            .Setup(x => x.LookupParty(It.IsAny<PartyLookup>()))
            .ReturnsAsync(
                (PartyLookup lookup) =>
                {
                    return lookup.Ssn is not null
                        ? new Party { SSN = lookup.Ssn, Name = "A person" }
                        : new Party
                        {
                            OrgNumber = lookup.OrgNo,
                            Organization = new Organization { Name = "An organisation", OrgNumber = lookup.OrgNo },
                        };
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
            x.Signee is PersonOnBehalfOfOrgSignee personOnBehalfOfOrgSignee
            && personOnBehalfOfOrgSignee.OnBehalfOfOrg.OrgNumber == org.OrgNumber
        );

        Assert.Equal(instance.Process.CurrentTask.ElementId, signeeContextWithMatchingSignatureDocument.TaskId);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument);
        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SigneeState);
        Assert.True(signeeContextWithMatchingSignatureDocument.SigneeState.IsAccessDelegated);

        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SignDocument);
        Assert.NotNull(signeeContextWithMatchingSignatureDocument.SignDocument?.SigneeInfo);
        Assert.Equal(
            org.OrgNumber,
            signeeContextWithMatchingSignatureDocument.SignDocument!.SigneeInfo!.OrganisationNumber
        );

        Assert.IsType<PersonOnBehalfOfOrgSignee>(signeeContextWithMatchingSignatureDocument.Signee);
        PersonOnBehalfOfOrgSignee personOnBehalfOfOrgSignee = (PersonOnBehalfOfOrgSignee)
            signeeContextWithMatchingSignatureDocument.Signee;

        Assert.NotNull(personOnBehalfOfOrgSignee.OnBehalfOfOrg);
        Assert.Equal(org.Name, personOnBehalfOfOrgSignee.OnBehalfOfOrg.OrgName);
        Assert.Equal(org.OrgNumber, personOnBehalfOfOrgSignee.OnBehalfOfOrg.OrgNumber);

        SigneeContext signatureWithOnTheFlySigneeContext = result.First(x =>
            x.Signee is PersonSignee personSignee && personSignee.SocialSecurityNumber == person.SSN
        );

        Assert.Equal(instance.Process.CurrentTask.ElementId, signatureWithOnTheFlySigneeContext.TaskId);

        Assert.NotNull(signatureWithOnTheFlySigneeContext);
        Assert.NotNull(signatureWithOnTheFlySigneeContext.SigneeState);
        Assert.True(signatureWithOnTheFlySigneeContext.SigneeState.IsAccessDelegated);

        Assert.NotNull(signatureWithOnTheFlySigneeContext.SignDocument);
        Assert.NotNull(signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo);
        Assert.Equal(person.SSN, signatureWithOnTheFlySigneeContext.SignDocument?.SigneeInfo?.PersonNumber);

        Assert.IsType<PersonSignee>(signatureWithOnTheFlySigneeContext.Signee);
        PersonSignee personSigneeOnTheFly = (PersonSignee)signatureWithOnTheFlySigneeContext.Signee;

        Assert.Equal(person.Name, personSigneeOnTheFly.FullName);
        Assert.Equal(person.SSN, personSigneeOnTheFly.SocialSecurityNumber);
    }

    [Fact]
    public async Task SynchronizeSigneeContextsWithSignDocuments_WithOnlySsn_MatchesCorrectSignDocument()
    {
        var ssn = "12345678910";

        List<SignDocument> testDocuments =
        [
            new SignDocument
            {
                SigneeInfo = new StorageSignee { PersonNumber = ssn, OrganisationNumber = null },
            },
        ];
        List<SigneeContext> testSigneeContexts =
        [
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new PersonSignee
                {
                    FullName = "Test Testesen",
                    SocialSecurityNumber = ssn,
                    Party = new Party { Name = "Test Testesen", SSN = ssn },
                },
            },
        ];

        await _signingService.SynchronizeSigneeContextsWithSignDocuments("Task_1", testSigneeContexts, testDocuments);

        Assert.Single(testSigneeContexts);
        Assert.NotNull(testSigneeContexts.First().SignDocument);
        Assert.True(testSigneeContexts.First().SignDocument?.SigneeInfo.PersonNumber == ssn);
        Assert.IsType<PersonSignee>(testSigneeContexts.First().Signee);
    }

    [Fact]
    public async Task SynchronizeSigneeContextsWithSignDocuments_WithOrgNrAndSsn_MatchesCorrectSignDocument()
    {
        var ssn = "12345678910";
        var orgNumber = "987654321";

        _altinnPartyClient
            .Setup(x => x.LookupParty(It.IsAny<PartyLookup>()))
            .ReturnsAsync(
                (PartyLookup lookup) =>
                {
                    return lookup.Ssn is not null
                        ? new Party
                        {
                            SSN = lookup.Ssn,
                            Person = new Person { SSN = lookup.Ssn },
                        }
                        : new Party
                        {
                            OrgNumber = lookup.OrgNo,
                            Organization = new Organization { OrgNumber = lookup.OrgNo },
                        };
                }
            );

        List<SignDocument> testDocuments = SetupSignDocuments(ssn, orgNumber);
        List<SigneeContext> testSigneeContexts =
        [
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new PersonOnBehalfOfOrgSignee
                {
                    FullName = "Test Testesen",
                    SocialSecurityNumber = ssn,
                    Party = new Party { Name = "Test Testesen", SSN = ssn },
                    OnBehalfOfOrg = new OrganisationSignee
                    {
                        OrgName = "TestOrg",
                        OrgNumber = orgNumber,
                        OrgParty = new Party { Name = "TestOrg", OrgNumber = orgNumber },
                    },
                },
            },
        ];

        await _signingService.SynchronizeSigneeContextsWithSignDocuments("Task_1", testSigneeContexts, testDocuments);

        Assert.Single(testSigneeContexts);
        Assert.NotNull(testSigneeContexts.First().SignDocument);
        Assert.True(testSigneeContexts.First().SignDocument?.SigneeInfo.PersonNumber == ssn);
        Assert.True(testSigneeContexts.First().SignDocument?.SigneeInfo.OrganisationNumber == orgNumber);
        Assert.IsType<PersonOnBehalfOfOrgSignee>(testSigneeContexts.First().Signee);
    }

    [Fact]
    public async Task SynchronizeSigneeContextsWithSignDocuments_WithNonMatchingSsn_AppendsNewSigneeContext()
    {
        _altinnPartyClient
            .Setup(x => x.LookupParty(It.IsAny<PartyLookup>()))
            .ReturnsAsync(
                (PartyLookup lookup) =>
                {
                    return lookup.Ssn is not null
                        ? new Party
                        {
                            SSN = lookup.Ssn,
                            Person = new Person { SSN = lookup.Ssn },
                        }
                        : new Party
                        {
                            OrgNumber = lookup.OrgNo,
                            Organization = new Organization { OrgNumber = lookup.OrgNo },
                        };
                }
            );

        var ssn = "12345678910";
        var orgNumber = "987654321";

        List<SignDocument> testDocuments = SetupSignDocuments(ssn, orgNumber);
        List<SigneeContext> testSigneeContexts =
        [
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new PersonOnBehalfOfOrgSignee
                {
                    FullName = "Test Testesen",
                    SocialSecurityNumber = "11111111111",
                    Party = new Party { Name = "Test Testesen" },
                    OnBehalfOfOrg = new OrganisationSignee
                    {
                        OrgName = "TestOrg",
                        OrgNumber = orgNumber,
                        OrgParty = new Party { Name = "TestOrg", OrgNumber = orgNumber },
                    },
                },
            },
        ];

        await _signingService.SynchronizeSigneeContextsWithSignDocuments("Task_1", testSigneeContexts, testDocuments);

        Assert.Equal(2, testSigneeContexts.Count);
        Assert.NotNull(testSigneeContexts[1].SignDocument);
        Assert.True(testSigneeContexts[1].SignDocument?.SigneeInfo.PersonNumber == ssn);
        Assert.True(testSigneeContexts[1].SignDocument?.SigneeInfo.OrganisationNumber == orgNumber);
    }

    [Fact]
    public async Task SynchronizeSigneeContextsWithSignDocuments_WithOrgAndSystemUserId_MatchesCorrectSigneeContext()
    {
        var orgNumber = "987654321";
        var systemUserId = Guid.NewGuid();

        List<SignDocument> testDocuments =
        [
            new SignDocument
            {
                SigneeInfo = new StorageSignee { SystemUserId = systemUserId, OrganisationNumber = orgNumber },
            },
        ];
        List<SigneeContext> testSigneeContexts =
        [
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new OrganisationSignee
                {
                    OrgName = "TestOrg",
                    OrgNumber = orgNumber,
                    OrgParty = new Party { Name = "TestOrg", OrgNumber = orgNumber },
                },
            },
        ];

        await _signingService.SynchronizeSigneeContextsWithSignDocuments("Task_1", testSigneeContexts, testDocuments);

        Assert.Single(testSigneeContexts);
        Assert.NotNull(testSigneeContexts.First().SignDocument);
        Assert.Null(testSigneeContexts.First().SignDocument?.SigneeInfo.PersonNumber);
        Assert.True(testSigneeContexts.First().SignDocument?.SigneeInfo.OrganisationNumber == orgNumber);
        Assert.True(testSigneeContexts.First().SignDocument?.SigneeInfo.SystemUserId == systemUserId);
        Assert.IsType<SystemSignee>(testSigneeContexts.First().Signee);
    }

    [Fact]
    public async Task SynchronizeSigneeContextsWithSignDocuments_WithMultiplePersonOrgAndSystemSignatures_MatchesCorrectSignatureContexts()
    {
        _altinnPartyClient
            .Setup(x => x.LookupParty(It.IsAny<PartyLookup>()))
            .ReturnsAsync(
                (PartyLookup lookup) =>
                {
                    return lookup.Ssn is not null
                        ? new Party
                        {
                            SSN = lookup.Ssn,
                            Person = new Person { SSN = lookup.Ssn },
                        }
                        : new Party
                        {
                            OrgNumber = lookup.OrgNo,
                            Organization = new Organization { OrgNumber = lookup.OrgNo },
                        };
                }
            );

        var systemUserId1 = Guid.NewGuid();
        var systemUserId2 = Guid.NewGuid();

        var ssn1 = "12345678910";
        var ssn2 = "11111111111";

        var orgNumber1 = "987654321";
        var orgNumber2 = "987654322";
        var unmatchedOrgNumber = "12324323423";

        List<SignDocument> signDocuments =
        [
            new SignDocument
            {
                SigneeInfo = new StorageSignee { PersonNumber = ssn1, OrganisationNumber = null },
            },
            new SignDocument
            {
                SigneeInfo = new StorageSignee { PersonNumber = ssn2, OrganisationNumber = null },
            },
            new SignDocument
            {
                SigneeInfo = new StorageSignee { PersonNumber = ssn1, OrganisationNumber = orgNumber1 },
            },
            new SignDocument
            {
                SigneeInfo = new StorageSignee { PersonNumber = ssn2, OrganisationNumber = orgNumber1 },
            },
            new SignDocument
            {
                SigneeInfo = new StorageSignee { SystemUserId = systemUserId1, OrganisationNumber = orgNumber1 },
            },
            new SignDocument
            {
                SigneeInfo = new StorageSignee { SystemUserId = systemUserId2, OrganisationNumber = orgNumber2 },
            },
        ];

        List<SigneeContext> signeeContexts =
        [
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new OrganisationSignee
                {
                    OrgName = "TestOrg 2",
                    OrgNumber = orgNumber2,
                    OrgParty = new Party { Name = "TestOrg 2", OrgNumber = orgNumber2 },
                },
            },
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new OrganisationSignee
                {
                    OrgName = "TestOrg 1",
                    OrgNumber = orgNumber1,
                    OrgParty = new Party { Name = "TestOrg 1", OrgNumber = orgNumber1 },
                },
            },
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new OrganisationSignee
                {
                    OrgName = "TestOrg 1",
                    OrgNumber = orgNumber1,
                    OrgParty = new Party { Name = "TestOrg 1", OrgNumber = orgNumber1 },
                },
            },
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new OrganisationSignee
                {
                    OrgName = "TestOrg 1",
                    OrgNumber = orgNumber1,
                    OrgParty = new Party { Name = "TestOrg 1", OrgNumber = orgNumber1 },
                },
            },
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new PersonSignee
                {
                    FullName = "Test Testesen 2",
                    SocialSecurityNumber = ssn2,
                    Party = new Party { Name = "Test Testesen 2", SSN = ssn2 },
                },
            },
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new PersonSignee
                {
                    FullName = "Test Testesen 1",
                    SocialSecurityNumber = ssn1,
                    Party = new Party { Name = "Test Testesen 1", SSN = ssn1 },
                },
            },
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new OrganisationSignee
                {
                    OrgName = "Unmatched Org",
                    OrgNumber = unmatchedOrgNumber,
                    OrgParty = new Party { Name = "Unmatched Org", OrgNumber = unmatchedOrgNumber },
                },
            },
        ];

        await _signingService.SynchronizeSigneeContextsWithSignDocuments("Task_1", signeeContexts, signDocuments);

        List<SystemSignee> systemSignees =
        [
            .. signeeContexts.Where(x => x.Signee is SystemSignee).Select(x => (SystemSignee)x.Signee),
        ];
        List<OrganisationSignee> orgSignees =
        [
            .. signeeContexts.Where(x => x.Signee is OrganisationSignee).Select(x => (OrganisationSignee)x.Signee),
        ];
        List<PersonSignee> personSignees =
        [
            .. signeeContexts.Where(x => x.Signee is PersonSignee).Select(x => (PersonSignee)x.Signee),
        ];
        List<PersonOnBehalfOfOrgSignee> personOnBehalfOfOrgSignees =
        [
            .. signeeContexts
                .Where(x => x.Signee is PersonOnBehalfOfOrgSignee)
                .Select(x => (PersonOnBehalfOfOrgSignee)x.Signee),
        ];

        Assert.Equal(7, signeeContexts.Count);
        Assert.Equal(2, systemSignees.Count);
        Assert.Single(orgSignees);
        Assert.Equal(2, personSignees.Count);
        Assert.Equal(2, personOnBehalfOfOrgSignees.Count);

        Assert.NotNull(systemSignees.Find(x => x.SystemId == systemUserId1 && x.OnBehalfOfOrg.OrgNumber == orgNumber1));
        Assert.NotNull(systemSignees.Find(x => x.SystemId == systemUserId2 && x.OnBehalfOfOrg.OrgNumber == orgNumber2));
        Assert.NotNull(orgSignees.Find(x => x.OrgNumber == unmatchedOrgNumber));
        Assert.NotNull(personSignees.Find(x => x.SocialSecurityNumber == ssn1));
        Assert.NotNull(personSignees.Find(x => x.SocialSecurityNumber == ssn2));
        Assert.NotNull(
            personOnBehalfOfOrgSignees.Find(x =>
                x.SocialSecurityNumber == ssn1 && x.OnBehalfOfOrg.OrgNumber == orgNumber1
            )
        );
        Assert.NotNull(
            personOnBehalfOfOrgSignees.Find(x =>
                x.SocialSecurityNumber == ssn2 && x.OnBehalfOfOrg.OrgNumber == orgNumber1
            )
        );
    }

    [Fact]
    public async Task ShouldReturnCorrectly()
    {
        _altinnPartyClient
            .Setup(x => x.LookupParty(It.IsAny<PartyLookup>()))
            .ReturnsAsync(
                (PartyLookup lookup) =>
                {
                    return lookup.Ssn is not null
                        ? new Party
                        {
                            SSN = lookup.Ssn,
                            Person = new Person { SSN = lookup.Ssn },
                        }
                        : new Party
                        {
                            OrgNumber = lookup.OrgNo,
                            Organization = new Organization { OrgNumber = lookup.OrgNo },
                        };
                }
            );

        var ssn = "12345678910";
        var orgNumber = "987654321";

        List<SignDocument> testDocuments = SetupSignDocuments(ssn, orgNumber);
        List<SigneeContext> testSigneeContexts =
        [
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new OrganisationSignee
                {
                    OrgName = "TestOrg",
                    OrgNumber = orgNumber,
                    OrgParty = new Party { Name = "TestOrg", OrgNumber = orgNumber },
                },
            },
            new SigneeContext
            {
                TaskId = "Task_1",
                SigneeState = new SigneeState(),
                Signee = new PersonOnBehalfOfOrgSignee
                {
                    FullName = "Test Testesen",
                    SocialSecurityNumber = ssn,
                    Party = new Party { Name = "Test Testesen", SSN = ssn },
                    OnBehalfOfOrg = new OrganisationSignee
                    {
                        OrgName = "TestOrg",
                        OrgNumber = orgNumber,
                        OrgParty = new Party { Name = "TestOrg", OrgNumber = orgNumber },
                    },
                },
            },
        ];

        await _signingService.SynchronizeSigneeContextsWithSignDocuments("Task_1", testSigneeContexts, testDocuments);
        Assert.Equal(2, testSigneeContexts.Count);
        Assert.True(testSigneeContexts.Find(x => x.Signee is OrganisationSignee) is not null);
        Assert.True(testSigneeContexts.Find(x => x.Signee is PersonOnBehalfOfOrgSignee) is not null);
    }

    private static List<SignDocument> SetupSignDocuments(string ssn, string? orgNumber = null)
    {
        List<SignDocument> testDocuments =
        [
            new SignDocument
            {
                SigneeInfo = new StorageSignee { PersonNumber = ssn, OrganisationNumber = orgNumber },
            },
        ];

        return testDocuments;
    }

    [Fact]
    public void RemoveSingingData_Removes_SigneeState()
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

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        var instance = new Instance { Data = [signeeStateDataElement] };
        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        _signingService.RemoveSigneeState(cachedInstanceMutator.Object, signatureConfiguration.SigneeStatesDataTypeId!);

        cachedInstanceMutator.Verify(x => x.Instance);
        cachedInstanceMutator.Verify(x => x.RemoveDataElement(signeeStateDataElement), Times.Once);
        cachedInstanceMutator.VerifyNoOtherCalls();
    }

    [Fact]
    public void RemoveAllSignatures_Removes_Signatures()
    {
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeStatesDataTypeId = "signeeStates",
            SignatureDataType = "signature",
        };

        var signatureDataElement = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = signatureConfiguration.SignatureDataType,
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        var instance = new Instance { Data = [signatureDataElement] };
        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        _signingService.RemoveAllSignatures(cachedInstanceMutator.Object, signatureConfiguration.SignatureDataType!);

        cachedInstanceMutator.Verify(x => x.Instance);
        cachedInstanceMutator.Verify(x => x.RemoveDataElement(signatureDataElement), Times.Once);
        cachedInstanceMutator.VerifyNoOtherCalls();
    }

    [Fact]
    public void RemoveSingingData_Does_Nothing_If_No_Existing_Data()
    {
        var signatureConfiguration = new AltinnSignatureConfiguration
        {
            SigneeStatesDataTypeId = "signeeStates",
            SignatureDataType = "signature",
        };

        var cachedInstanceMutator = new Mock<IInstanceDataMutator>();
        var instance = new Instance { Data = [] };
        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);

        _signingService.RemoveSigneeState(cachedInstanceMutator.Object, signatureConfiguration.SigneeStatesDataTypeId!);

        cachedInstanceMutator.Verify(x => x.Instance);
        cachedInstanceMutator.VerifyNoOtherCalls();
    }

    private static byte[] ToBytes<T>(T obj)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
    }
}

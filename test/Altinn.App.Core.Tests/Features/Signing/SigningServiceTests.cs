using System.Text;
using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
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

        Instance instance = new()
        {
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "Task_1" } },
            Data = [signeeStateDataElement, signDocumentDataElement],
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

        var signDocument = new SignDocument
        {
            SigneeInfo = new Signee { OrganisationNumber = signeeState.First().Party.Organization.OrgNumber },
        };

        cachedInstanceMutator.Setup(x => x.Instance).Returns(instance);
        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signeeStateDataElement.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signeeState)));

        cachedInstanceMutator
            .Setup(x => x.GetBinaryData(new DataElementIdentifier(signDocumentDataElement.Id)))
            .ReturnsAsync(new ReadOnlyMemory<byte>(ToBytes(signDocument)));

        // Act
        List<SigneeContext> result = await _signingService.GetSigneeContexts(
            cachedInstanceMutator.Object,
            signatureConfiguration
        );

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        SigneeContext signeeContext = result.First();
        signeeContext.Should().NotBeNull();
        signeeContext.TaskId.Should().Be(instance.Process.CurrentTask.ElementId);

        signeeContext.OrganisationSignee.Should().NotBeNull();
        signeeContext.OrganisationSignee?.DisplayName.Should().Be(org.Name);
        signeeContext.OrganisationSignee?.OrganisationNumber.Should().Be(org.OrgNumber);

        signeeContext.Party.Should().NotBeNull();
        signeeContext.Party.Organization.Should().NotBeNull();
        signeeContext.Party.Organization?.OrgNumber.Should().Be(org.OrgNumber);
        signeeContext.Party.Organization?.Name.Should().Be(org.Name);

        signeeContext.SigneeState.Should().NotBeNull();
        signeeContext.SigneeState.IsAccessDelegated.Should().BeTrue();

        signeeContext.SignDocument.Should().NotBeNull();
        signeeContext.SignDocument?.SigneeInfo.Should().NotBeNull();
        signeeContext.SignDocument?.SigneeInfo?.OrganisationNumber.Should().Be(org.OrgNumber);
    }

    private static byte[] ToBytes<T>(T obj)
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
    }
}

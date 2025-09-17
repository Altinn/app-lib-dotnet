using System.Diagnostics;
using System.Text;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Features.Signing.Services;
using Altinn.App.Core.Features.Validation.Default;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using CoreSignee = Altinn.App.Core.Features.Signing.Models.Signee;
using SigneeState = Altinn.App.Core.Features.Signing.Models.SigneeContextState;

namespace Altinn.App.Core.Tests.Features.Validators.Default;

public class SignatureHashValidatorTests
{
    private readonly Mock<IProcessReader> _processReaderMock = new();
    private readonly Mock<ISigningService> _signingServiceMock = new();
    private readonly Mock<IDataClient> _dataClientMock = new();
    private readonly Mock<IAppMetadata> _appMetadataMock = new();
    private readonly Mock<ILogger<SignatureHashValidator>> _loggerMock = new();
    private readonly Mock<IInstanceDataAccessor> _dataAccessorMock = new();
    private readonly SignatureHashValidator _validator;

    public SignatureHashValidatorTests()
    {
        _validator = new SignatureHashValidator(
            _signingServiceMock.Object,
            _processReaderMock.Object,
            _dataClientMock.Object,
            _appMetadataMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Validate_WithValidSignatureHashes_ReturnsEmptyList()
    {
        var testData = "test data";
        var expectedHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";
        var instance = CreateTestInstance();
        var signingConfiguration = new AltinnSignatureConfiguration { SignatureDataType = "signature" };
        var applicationMetadata = new ApplicationMetadata("testorg/testapp")
        {
            DataTypes = [new DataType { Id = "form", ActionRequiredToRead = null }],
        };
        var signeeContext = CreateSigneeContextWithValidHash(expectedHash);

        SetupMocks(instance, signingConfiguration, applicationMetadata, [signeeContext], testData);

        var result = await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithInvalidSignatureHash_ReturnsValidationIssue()
    {
        var testData = "test data";
        var storedHash = "different-hash";
        var instance = CreateTestInstance();
        var signingConfiguration = new AltinnSignatureConfiguration { SignatureDataType = "signature" };
        var applicationMetadata = new ApplicationMetadata("testorg/testapp")
        {
            DataTypes = [new DataType { Id = "form", ActionRequiredToRead = null }],
        };
        var signeeContext = CreateSigneeContextWithValidHash(storedHash);

        SetupMocks(instance, signingConfiguration, applicationMetadata, [signeeContext], testData);

        var result = await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        result.Should().HaveCount(1);
        result[0].Code.Should().Be(ValidationIssueCodes.DataElementCodes.InvalidSignatureHash);
        result[0].Severity.Should().Be(ValidationIssueSeverity.Error);
        result[0].Description.Should().Be(ValidationIssueCodes.DataElementCodes.InvalidSignatureHash);
    }

    [Fact]
    public async Task Validate_WithMissingSignatureConfiguration_ThrowsApplicationConfigException()
    {
        var instance = CreateTestInstance();
        _dataAccessorMock.Setup(x => x.Instance).Returns(instance);
        _processReaderMock
            .Setup(x => x.GetAltinnTaskExtension("signing-task"))
            .Returns(new AltinnTaskExtension { SignatureConfiguration = null });

        var action = async () => await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        await action
            .Should()
            .ThrowAsync<ApplicationConfigException>()
            .WithMessage("Signing configuration not found in AltinnTaskExtension");
    }

    [Fact]
    public async Task Validate_WithRestrictedReadDataType_UsesServiceOwnerAuth()
    {
        var testData = "test data";
        var expectedHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";
        var instance = CreateTestInstance();
        var signingConfiguration = new AltinnSignatureConfiguration { SignatureDataType = "signature" };
        var applicationMetadata = new ApplicationMetadata("testorg/testapp")
        {
            DataTypes = [new DataType { Id = "form", ActionRequiredToRead = "read" }],
        };
        var signeeContext = CreateSigneeContextWithValidHash(expectedHash);

        SetupMocks(instance, signingConfiguration, applicationMetadata, [signeeContext], testData);

        await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        _dataClientMock.Verify(
            x =>
                x.GetBinaryData(
                    "testorg",
                    "testapp",
                    12345,
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.Is<StorageAuthenticationMethod?>(auth => auth != null),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Validate_WithNonRestrictedReadDataType_DoesNotUseServiceOwnerAuth()
    {
        var testData = "test data";
        var expectedHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";
        var instance = CreateTestInstance();
        var signingConfiguration = new AltinnSignatureConfiguration { SignatureDataType = "signature" };
        var applicationMetadata = new ApplicationMetadata("testorg/testapp")
        {
            DataTypes = [new DataType { Id = "form", ActionRequiredToRead = null }],
        };
        var signeeContext = CreateSigneeContextWithValidHash(expectedHash);

        SetupMocks(instance, signingConfiguration, applicationMetadata, [signeeContext], testData);

        await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        _dataClientMock.Verify(
            x =>
                x.GetBinaryData(
                    "testorg",
                    "testapp",
                    12345,
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    null,
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Validate_WithDataTypeNotFoundInApplicationMetadata_ThrowsApplicationConfigException()
    {
        var testData = "test data";
        var expectedHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";
        var instance = CreateTestInstance();
        var signingConfiguration = new AltinnSignatureConfiguration { SignatureDataType = "signature" };
        var applicationMetadata = new ApplicationMetadata("testorg/testapp") { DataTypes = [] };
        var signeeContext = CreateSigneeContextWithValidHash(expectedHash);

        SetupMocks(instance, signingConfiguration, applicationMetadata, [signeeContext], testData);

        var action = async () => await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        await action
            .Should()
            .ThrowAsync<ApplicationConfigException>()
            .WithMessage(
                "Unable to find data type form for data element 550e8400-e29b-41d4-a716-446655440001 in applicationmetadata.json."
            );
    }

    [Fact]
    public async Task Validate_WithMultipleSigneeContexts_ValidatesAllSignatures()
    {
        var testData = "test data";
        var expectedHash = "916f0027a575074ce72a331777c3478d6513f786a591bd892da1a577bf2335f9";
        var instance = CreateTestInstance();
        var signingConfiguration = new AltinnSignatureConfiguration { SignatureDataType = "signature" };
        var applicationMetadata = new ApplicationMetadata("testorg/testapp")
        {
            DataTypes = [new DataType { Id = "form", ActionRequiredToRead = null }],
        };
        var signeeContexts = new List<SigneeContext>
        {
            CreateSigneeContextWithValidHash(expectedHash),
            CreateSigneeContextWithValidHash(expectedHash),
        };

        SetupMocks(instance, signingConfiguration, applicationMetadata, signeeContexts, testData);

        var result = await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        result.Should().BeEmpty();
        _dataClientMock.Verify(
            x =>
                x.GetBinaryData(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<StorageAuthenticationMethod?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(2)
        );
    }

    [Fact]
    public async Task Validate_WithSigneeContextWithoutSignDocument_SkipsValidation()
    {
        var instance = CreateTestInstance();
        var signingConfiguration = new AltinnSignatureConfiguration { SignatureDataType = "signature" };
        var applicationMetadata = new ApplicationMetadata("testorg/testapp")
        {
            DataTypes = [new DataType { Id = "form", ActionRequiredToRead = null }],
        };
        var signeeContext = new SigneeContext
        {
            TaskId = "signing-task",
            Signee = new CoreSignee.PersonSignee
            {
                SocialSecurityNumber = "12345678901",
                FullName = "Test Person",
                Party = new Altinn.Platform.Register.Models.Party(),
            },
            SigneeState = new SigneeState { IsAccessDelegated = false },
            SignDocument = null,
        };

        SetupMocks(instance, signingConfiguration, applicationMetadata, [signeeContext], "test");

        var result = await _validator.Validate(_dataAccessorMock.Object, "signing-task", "en");

        result.Should().BeEmpty();
        _dataClientMock.Verify(
            x =>
                x.GetBinaryData(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<StorageAuthenticationMethod?>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public void TaskId_ShouldReturnAsterisk()
    {
        _validator.TaskId.Should().Be("*");
    }

    [Fact]
    public void NoIncrementalValidation_ShouldReturnTrue()
    {
        _validator.NoIncrementalValidation.Should().BeTrue();
    }

    [Fact]
    public void ShouldRunForTask_WithSigningTask_ReturnsTrue()
    {
        var taskConfig = new AltinnTaskExtension { TaskType = "signing" };
        _processReaderMock.Setup(x => x.GetAltinnTaskExtension("signing-task")).Returns(taskConfig);

        var result = _validator.ShouldRunForTask("signing-task");

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRunForTask_WithNonSigningTask_ReturnsFalse()
    {
        var taskConfig = new AltinnTaskExtension { TaskType = "data" };
        _processReaderMock.Setup(x => x.GetAltinnTaskExtension("data-task")).Returns(taskConfig);

        var result = _validator.ShouldRunForTask("data-task");

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRunForTask_WithNullTaskType_ReturnsFalse()
    {
        var taskConfig = new AltinnTaskExtension { TaskType = null };
        _processReaderMock.Setup(x => x.GetAltinnTaskExtension("task")).Returns(taskConfig);

        var result = _validator.ShouldRunForTask("task");

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRunForTask_WithException_ReturnsFalse()
    {
        _processReaderMock
            .Setup(x => x.GetAltinnTaskExtension("task"))
            .Throws(new InvalidOperationException("Task not found"));

        var result = _validator.ShouldRunForTask("task");

        result.Should().BeFalse();
    }

    [Fact]
    public void HasRelevantChanges_ShouldThrowUnreachableException()
    {
        var changes = new DataElementChanges([]);

        var action = () => _validator.HasRelevantChanges(_dataAccessorMock.Object, "task", changes);

        action
            .Should()
            .ThrowAsync<UnreachableException>()
            .WithMessage("HasRelevantChanges should not be called because NoIncrementalValidation is true.");
    }

    private Instance CreateTestInstance()
    {
        return new Instance
        {
            Id = "12345/550e8400-e29b-41d4-a716-446655440000",
            Org = "testorg",
            AppId = "testapp",
            Data = [new DataElement { Id = "550e8400-e29b-41d4-a716-446655440001", DataType = "form" }],
        };
    }

    private SigneeContext CreateSigneeContextWithValidHash(string hash)
    {
        return new SigneeContext
        {
            TaskId = "signing-task",
            Signee = new CoreSignee.PersonSignee
            {
                SocialSecurityNumber = "12345678901",
                FullName = "Test Person",
                Party = new Altinn.Platform.Register.Models.Party(),
            },
            SigneeState = new SigneeState { IsAccessDelegated = false },
            SignDocument = new SignDocument
            {
                DataElementSignatures =
                [
                    new SignDocument.DataElementSignature
                    {
                        DataElementId = "550e8400-e29b-41d4-a716-446655440001",
                        Sha256Hash = hash,
                    },
                ],
            },
        };
    }

    private void SetupMocks(
        Instance instance,
        AltinnSignatureConfiguration signingConfiguration,
        ApplicationMetadata applicationMetadata,
        List<SigneeContext> signeeContexts,
        string testData
    )
    {
        _dataAccessorMock.Setup(x => x.Instance).Returns(instance);

        _processReaderMock
            .Setup(x => x.GetAltinnTaskExtension("signing-task"))
            .Returns(new AltinnTaskExtension { SignatureConfiguration = signingConfiguration });

        _appMetadataMock.Setup(x => x.GetApplicationMetadata()).ReturnsAsync(applicationMetadata);

        _signingServiceMock
            .Setup(x =>
                x.GetSigneeContexts(
                    It.Is<IInstanceDataAccessor>(d => d == _dataAccessorMock.Object),
                    It.Is<AltinnSignatureConfiguration>(c => c == signingConfiguration),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(signeeContexts);

        _dataClientMock
            .Setup(x =>
                x.GetBinaryData(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<StorageAuthenticationMethod?>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(() => new MemoryStream(Encoding.UTF8.GetBytes(testData)));
    }
}

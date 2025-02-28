using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Process.ProcessTasks;

public class SigningProcessTaskTests
{
    private readonly Mock<IProcessReader> _processReaderMock;
    private readonly Mock<ISigningService> _signingServiceMock;
    private readonly SigningProcessTask _paymentProcessTask;

    public SigningProcessTaskTests()
    {
        _processReaderMock = new Mock<IProcessReader>();
        _signingServiceMock = new Mock<ISigningService>();

        _paymentProcessTask = new SigningProcessTask(
            _signingServiceMock.Object,
            _processReaderMock.Object,
            new Mock<IAppMetadata>().Object,
            new Mock<IHostEnvironment>().Object,
            new Mock<IDataClient>().Object,
            new Mock<IInstanceClient>().Object,
            new ModelSerializationService(new Mock<IAppModel>().Object),
            new Mock<IPdfService>().Object
        );
    }

    [Fact]
    public async Task Start_ShouldDeleteExistingSigningData()
    {
        Instance instance = CreateInstance();
        string taskId = instance.Process.CurrentTask.ElementId;

        var altinnTaskExtension = new AltinnTaskExtension { SignatureConfiguration = CreateSigningConfiguration() };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>())).Returns(altinnTaskExtension);
        _signingServiceMock
            .Setup(x =>
                x.GenerateSigneeContexts(
                    It.IsAny<IInstanceDataMutator>(),
                    It.IsAny<AltinnSignatureConfiguration>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        // Act
        await _paymentProcessTask.Start(taskId, instance);

        // Assert
        _signingServiceMock.Verify(
            x =>
                x.GenerateSigneeContexts(
                    It.IsAny<IInstanceDataMutator>(),
                    It.IsAny<AltinnSignatureConfiguration>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _signingServiceMock.Verify(
            x =>
                x.InitialiseSignees(
                    taskId,
                    It.IsAny<IInstanceDataMutator>(),
                    It.IsAny<List<SigneeContext>>(),
                    It.IsAny<AltinnSignatureConfiguration>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _signingServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Abandon_ShouldDeleteExistingSigningData()
    {
        Instance instance = CreateInstance();
        string taskId = instance.Process.CurrentTask.ElementId;

        var altinnTaskExtension = new AltinnTaskExtension { SignatureConfiguration = CreateSigningConfiguration() };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>())).Returns(altinnTaskExtension);

        // Act
        await _paymentProcessTask.Abandon(taskId, instance);

        // Assert
        _signingServiceMock.Verify(x =>
            x.AbortRuntimeDelegatedSigning(
                taskId,
                It.IsAny<IInstanceDataMutator>(),
                altinnTaskExtension.SignatureConfiguration,
                It.IsAny<CancellationToken>()
            )
        );
    }

    private static Instance CreateInstance()
    {
        return new Instance()
        {
            Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d",
            AppId = "ttd/test",
            Process = new ProcessState
            {
                CurrentTask = new ProcessElementInfo { AltinnTaskType = "signing", ElementId = "Task_1" },
            },
            Data = [],
        };
    }

    private static AltinnSignatureConfiguration CreateSigningConfiguration()
    {
        return new AltinnSignatureConfiguration
        {
            SignatureDataType = "SignatureDataType",
            SigneeStatesDataTypeId = "SigneeStatesDataTypeId",
            SigneeProviderId = "SigneeProviderId",
        };
    }
}

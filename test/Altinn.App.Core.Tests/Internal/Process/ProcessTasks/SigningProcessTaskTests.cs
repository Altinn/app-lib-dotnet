using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            new Mock<HttpContextAccessor>().Object,
            new Mock<IProfileClient>().Object,
            new Mock<IAltinnPartyClient>().Object,
            new Mock<IOptions<GeneralSettings>>().Object,
            new Mock<ILogger<SigningProcessTask>>().Object
        );
    }

    [Fact]
    public async Task Start_ShouldDeleteExistingSigneeState()
    {
        Instance instance = CreateInstance();
        string taskId = instance.Process.CurrentTask.ElementId;

        var altinnTaskExtension = new AltinnTaskExtension { SignatureConfiguration = CreateSigningConfiguration() };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>())).Returns(altinnTaskExtension);
        _signingServiceMock
            .Setup(x =>
                x.InitializeSignees(
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
            x => x.DeleteSigneeState(It.IsAny<IInstanceDataMutator>(), altinnTaskExtension.SignatureConfiguration),
            Times.Once
        );
    }

    [Fact]
    public async Task Abandon_ShouldDeleteExistingSigneeState()
    {
        Instance instance = CreateInstance();
        string taskId = instance.Process.CurrentTask.ElementId;

        var altinnTaskExtension = new AltinnTaskExtension { SignatureConfiguration = CreateSigningConfiguration() };

        _processReaderMock.Setup(x => x.GetAltinnTaskExtension(It.IsAny<string>())).Returns(altinnTaskExtension);

        // Act
        await _paymentProcessTask.Abandon(taskId, instance);

        // Assert
        _signingServiceMock.Verify(x =>
            x.DeleteSigneeState(It.IsAny<IInstanceDataMutator>(), altinnTaskExtension.SignatureConfiguration)
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

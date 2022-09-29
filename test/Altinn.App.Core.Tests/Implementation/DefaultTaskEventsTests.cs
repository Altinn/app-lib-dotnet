using System;
using System.Collections.Generic;
using Altinn.App.Core.Features;
using Altinn.App.Core.Implementation;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Pdf;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Implementation;

public class DefaultTaskEventsTests: IDisposable
{
    private ILogger<DefaultTaskEvents> logger = NullLogger<DefaultTaskEvents>.Instance;
    private Mock<IAppResources> resMock;
    private Mock<Application> applicationMock;
    private Mock<IData> dataMock;
    private Mock<IPrefill> prefillMock;
    private IAppModel appModel;
    private Mock<IInstantiationProcessor> instantiationMock;
    private Mock<IInstance> instanceMock;
    private IEnumerable<IProcessTaskEnd> taskEnds;
    private IEnumerable<IProcessTaskAbandon> taskAbandons;
    private Mock<IPdfService> pdfMock;

    public DefaultTaskEventsTests()
    {
        applicationMock = new Mock<Application>();
        resMock = new Mock<IAppResources>();
        resMock.Setup(r => r.GetApplication()).Returns(applicationMock.Object);
        dataMock = new Mock<IData>();
        prefillMock = new Mock<IPrefill>();
        appModel = new DefaultAppModel(NullLogger<DefaultAppModel>.Instance);
        instantiationMock = new Mock<IInstantiationProcessor>();
        instanceMock = new Mock<IInstance>();
        taskEnds = new List<IProcessTaskEnd>();
        taskAbandons = new List<IProcessTaskAbandon>();
        pdfMock = new Mock<IPdfService>();
    }

    [Fact]
    public async void OnAbandonProcessTask_handles_no_IProcessTaskAbandon_injected()
    {
        DefaultTaskEvents te = new DefaultTaskEvents(
            logger,
            resMock.Object,
            dataMock.Object,
            prefillMock.Object,
            appModel,
            instantiationMock.Object,
            instanceMock.Object,
            taskEnds,
            taskAbandons,
            pdfMock.Object);
        te.OnAbandonProcessTask("Task_1", new Instance());
    }
    
    [Fact]
    public async void OnAbandonProcessTask_calls_all_added_implementations()
    {
        Mock<IProcessTaskAbandon> abandonOne = new Mock<IProcessTaskAbandon>();
        Mock<IProcessTaskAbandon> abandonTwo = new Mock<IProcessTaskAbandon>();
        taskAbandons = new List<IProcessTaskAbandon>() { abandonOne.Object, abandonTwo.Object };
        DefaultTaskEvents te = new DefaultTaskEvents(
            logger,
            resMock.Object,
            dataMock.Object,
            prefillMock.Object,
            appModel,
            instantiationMock.Object,
            instanceMock.Object,
            taskEnds,
            taskAbandons,
            pdfMock.Object);
        var instance = new Instance();
        te.OnAbandonProcessTask("Task_1", instance);
        abandonOne.Verify(a => a.HandleEvent("Task_1", instance));
        abandonTwo.Verify(a => a.HandleEvent("Task_1", instance));
        abandonOne.VerifyNoOtherCalls();
        abandonTwo.VerifyNoOtherCalls();
    }

    public void Dispose()
    {
        resMock.Verify(r => r.GetApplication());
        resMock.VerifyNoOtherCalls();
        applicationMock.VerifyNoOtherCalls();
        dataMock.VerifyNoOtherCalls();
        prefillMock.VerifyNoOtherCalls();
        instantiationMock.VerifyNoOtherCalls();
        instanceMock.VerifyNoOtherCalls();
        pdfMock.VerifyNoOtherCalls();
    }
}
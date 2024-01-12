using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.EventHandlers.ProcessTask;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.Platform.Storage.Interface.Models;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.Process.EventHandlers.ProcessTask;

public class EndTaskEventHandlerTests
{
    private Mock<IProcessTaskDataLocker> _processTaskDataLocker;
    private Mock<IProcessTaskFinalizer> _processTaskFinisher;
    private Mock<IServiceTask> _pdfServiceTask;
    private Mock<IServiceTask> _eformidlingServiceTask;
    private IEnumerable<IProcessTaskEnd> _processTaskEnds;

    public EndTaskEventHandlerTests()
    {
        _processTaskDataLocker = new Mock<IProcessTaskDataLocker>();
        _processTaskFinisher = new Mock<IProcessTaskFinalizer>();
        _pdfServiceTask = new Mock<IServiceTask>();
        _eformidlingServiceTask = new Mock<IServiceTask>();
        _processTaskEnds = new List<IProcessTaskEnd>();
    }

    [Fact]
    public async Task Execute_handles_no_IProcessTaskAbandon_injected()
    {
        EndTaskEventHandler eteh = new EndTaskEventHandler(
            _processTaskDataLocker.Object,
            _processTaskFinisher.Object,
            _pdfServiceTask.Object,
            _eformidlingServiceTask.Object,
            _processTaskEnds);
        var instance = new Instance()
        {
            Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d",
            AppId = "ttd/test",
        };
        Mock<IProcessTask> mockProcessTask = new Mock<IProcessTask>();
        await eteh.Execute(mockProcessTask.Object, "Task_1", instance);
        _processTaskDataLocker.Verify(p => p.Lock("Task_1", instance));
        _processTaskFinisher.Verify(p => p.Finalize("Task_1", instance));
        _pdfServiceTask.Verify(p => p.Execute("Task_1", instance));
        _eformidlingServiceTask.Verify(p => p.Execute("Task_1", instance));
        mockProcessTask.Verify(p => p.End("Task_1", instance));

        _processTaskDataLocker.VerifyNoOtherCalls();
        _processTaskFinisher.VerifyNoOtherCalls();
        _pdfServiceTask.VerifyNoOtherCalls();
        _eformidlingServiceTask.VerifyNoOtherCalls();
        mockProcessTask.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_calls_all_added_implementations_of_IProcessTaskEnd()
    {
        Mock<IProcessTaskEnd> endOne = new Mock<IProcessTaskEnd>();
        Mock<IProcessTaskEnd> endTwo = new Mock<IProcessTaskEnd>();
        _processTaskEnds = new List<IProcessTaskEnd>() { endOne.Object, endTwo.Object };
        EndTaskEventHandler eteh = new EndTaskEventHandler(
            _processTaskDataLocker.Object,
            _processTaskFinisher.Object,
            _pdfServiceTask.Object,
            _eformidlingServiceTask.Object,
            _processTaskEnds);
        var instance = new Instance()
        {
            Id = "1337/fa0678ad-960d-4307-aba2-ba29c9804c9d",
            AppId = "ttd/test",
        };
        Mock<IProcessTask> mockProcessTask = new Mock<IProcessTask>();
        await eteh.Execute(mockProcessTask.Object, "Task_1", instance);
        endOne.Verify(a => a.End("Task_1", instance));
        endTwo.Verify(a => a.End("Task_1", instance));
        _processTaskDataLocker.Verify(p => p.Lock("Task_1", instance));
        _processTaskFinisher.Verify(p => p.Finalize("Task_1", instance));
        _pdfServiceTask.Verify(p => p.Execute("Task_1", instance));
        _eformidlingServiceTask.Verify(p => p.Execute("Task_1", instance));
        mockProcessTask.Verify(p => p.End("Task_1", instance));

        _processTaskDataLocker.VerifyNoOtherCalls();
        _processTaskFinisher.VerifyNoOtherCalls();
        _pdfServiceTask.VerifyNoOtherCalls();
        _eformidlingServiceTask.VerifyNoOtherCalls();
        mockProcessTask.VerifyNoOtherCalls();
        endOne.VerifyNoOtherCalls();
        endTwo.VerifyNoOtherCalls();
    }
}
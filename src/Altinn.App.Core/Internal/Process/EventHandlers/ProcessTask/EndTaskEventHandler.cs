using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Internal.Process.ProcessTasks.Common;
using Altinn.App.Core.Internal.Process.ProcessTasks.ServiceTasks.Legacy;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Internal.Process.EventHandlers.ProcessTask;

/// <summary>
/// This event handler is responsible for handling the end event for a process task.
/// </summary>
public class EndTaskEventHandler : IEndTaskEventHandler
{
    private readonly IProcessTaskDataLocker _processTaskDataLocker;
    private readonly IProcessTaskFinalizer _processTaskFinisher;
    private readonly AppImplementationFactory _appImplementationFactory;
    private readonly IPdfServiceTaskLegacy _pdfServiceTaskLegacy;
    private readonly IEFormidlingServiceTaskLegacy _eformidlingServiceTaskLegacy;
    private readonly ILogger<EndTaskEventHandler> _logger;

    /// <summary>
    /// This event handler is responsible for handling the end event for a process task.
    /// </summary>
    public EndTaskEventHandler(
        IProcessTaskDataLocker processTaskDataLocker,
        IProcessTaskFinalizer processTaskFinisher,
        IServiceProvider serviceProvider,
        ILogger<EndTaskEventHandler> logger
    )
    {
        _processTaskDataLocker = processTaskDataLocker;
        _processTaskFinisher = processTaskFinisher;
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
        _pdfServiceTaskLegacy = serviceProvider.GetRequiredService<IPdfServiceTaskLegacy>();
        _eformidlingServiceTaskLegacy = serviceProvider.GetRequiredService<IEFormidlingServiceTaskLegacy>();
        _logger = logger;
    }

    /// <summary>
    /// Execute the event handler logic.
    /// </summary>
    public async Task Execute(IProcessTask processTask, string taskId, Instance instance)
    {
        await processTask.End(taskId, instance);
        await _processTaskFinisher.Finalize(taskId, instance);
        await RunAppDefinedProcessTaskEndHandlers(taskId, instance);
        await _processTaskDataLocker.Lock(taskId, instance);

        //These two services are scheduled to be removed in a major version. Pdf and eFormidling have been implemented as service tasks and can be added to the app using the process bpmn file.
        try
        {
            await _pdfServiceTaskLegacy.Execute(taskId, instance);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing pdf service task. Unlocking data again.");
            await _processTaskDataLocker.Unlock(taskId, instance);
            throw;
        }

        try
        {
            await _eformidlingServiceTaskLegacy.Execute(taskId, instance);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error executing eFormidling service task. Unlocking data again.");
            await _processTaskDataLocker.Unlock(taskId, instance);
            throw;
        }
    }

    /// <summary>
    /// Runs IProcessTaskEnds defined in the app.
    /// </summary>
    private async Task RunAppDefinedProcessTaskEndHandlers(string endEvent, Instance instance)
    {
        var handlers = _appImplementationFactory.GetAll<IProcessTaskEnd>();
        foreach (IProcessTaskEnd taskEnd in handlers)
        {
            await taskEnd.End(endEvent, instance);
        }
    }
}

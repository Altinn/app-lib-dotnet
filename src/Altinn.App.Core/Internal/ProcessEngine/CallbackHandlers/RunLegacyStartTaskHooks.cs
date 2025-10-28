using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Internal.ProcessEngine.CallbackHandlers;

//TODO: Research to what degree TEs would accept a move to new hook interface
/// <summary>
/// Run the legacy IProcessTaskStart implementations defined in the app. No unit of work and rollback support.
/// </summary>
internal sealed class RunAppDefinedProcessTaskStartHandler : IProcessEngineCallbackHandler
{
    private readonly AppImplementationFactory _appImplementationFactory;
    public string Key => "RunAppDefinedProcessTaskStart";

    public RunAppDefinedProcessTaskStartHandler(IServiceProvider serviceProvider)
    {
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
    }

    public async Task<ProcessEngineCallbackHandlerResult> Execute(ProcessEngineCallbackHandlerParameters parameters)
    {
        IEnumerable<IProcessTaskStart> handlers = _appImplementationFactory.GetAll<IProcessTaskStart>();
        Instance instance = parameters.InstanceDataMutator.Instance;
        string? taskId = instance.Process.CurrentTask.ElementId;

        try
        {
            foreach (IProcessTaskStart processTaskStarts in handlers)
            {
                //TODO: How to get prefill??
                await processTaskStarts.Start(taskId, instance, []);
            }
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCallbackHandlerResult(ex);
        }

        return new SuccessfulProcessEngineCallbackHandlerResult();
    }
}

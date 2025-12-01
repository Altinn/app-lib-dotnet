using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

//TODO: Research to what degree TEs would accept a move to new hook interface
/// <summary>
/// Run the legacy IProcessTaskStart implementations defined in the app. No unit of work and rollback support.
/// </summary>
internal sealed class StartTaskLegacyHook : IProcessEngineCommand
{
    public static string Key => "StartTaskLegacyHook";

    public string GetKey() => Key;

    private readonly AppImplementationFactory _appImplementationFactory;

    public StartTaskLegacyHook(IServiceProvider serviceProvider)
    {
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        Instance instance = parameters.InstanceDataMutator.Instance;
        string? taskId = instance.Process.CurrentTask.ElementId;

        try
        {
            IEnumerable<IProcessTaskStart> handlers = _appImplementationFactory.GetAll<IProcessTaskStart>();

            foreach (IProcessTaskStart processTaskStarts in handlers)
            {
                //TODO: How to get prefill??
                await processTaskStarts.Start(taskId, instance, []);
            }
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }

        return new SuccessfulProcessEngineCommandResult();
    }
}

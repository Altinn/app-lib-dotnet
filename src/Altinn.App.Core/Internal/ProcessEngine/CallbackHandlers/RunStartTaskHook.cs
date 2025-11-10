using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.ProcessEngine.Hooks;
using Altinn.App.Core.Internal.ProcessEngine.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Internal.ProcessEngine.CallbackHandlers;

internal sealed class RunStartTaskHook : IProcessEngineCallbackHandler
{
    private readonly AppImplementationFactory _appImplementationFactory;
    public string Key => "RunStartTaskHook";

    public RunStartTaskHook(IServiceProvider serviceProvider)
    {
        _appImplementationFactory = serviceProvider.GetRequiredService<AppImplementationFactory>();
    }

    public async Task<ProcessEngineCallbackHandlerResult> Execute(ProcessEngineCallbackHandlerParameters parameters)
    {
        //TODO: One or multiple?
        IStartTask? hook = _appImplementationFactory.Get<IStartTask>();

        if (hook == null)
        {
            return new SuccessfulProcessEngineCallbackHandlerResult();
        }

        var hookParameters = new StartTaskParameters { InstanceDataMutator = parameters.InstanceDataMutator };

        await hook.ExecuteAsync(hookParameters);

        return new SuccessfulProcessEngineCallbackHandlerResult();
    }
}

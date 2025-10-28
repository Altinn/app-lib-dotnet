using Altinn.App.Api.Controllers;

namespace Altinn.App.Core.Internal.ProcessEngine;

internal interface IProcessEngineCallbackHandler
{
    string Key { get; }

    Task<ProcessEngineCallbackHandlerResult> Execute(ProcessEngineCallbackHandlerParameters parameters);
};

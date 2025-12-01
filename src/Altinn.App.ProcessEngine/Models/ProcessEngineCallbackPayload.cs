namespace Altinn.App.ProcessEngine.Models;

internal sealed record ProcessEngineCallbackPayload(ProcessEngineActor ProcessEngineActor, string Metadata);

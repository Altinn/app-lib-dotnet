namespace Altinn.App.ProcessEngine.Models;

public sealed record ProcessEngineCallbackPayload(ProcessEngineActor ProcessEngineActor, string Metadata);

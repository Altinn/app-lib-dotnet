namespace Altinn.App.ProcessEngine.Models;

public sealed record ProcessEngineAppCallbackPayload(ProcessEngineActor ProcessEngineActor, string? Metadata = null);

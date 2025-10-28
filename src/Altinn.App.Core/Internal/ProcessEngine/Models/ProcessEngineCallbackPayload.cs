using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Models;

public sealed record ProcessEngineCallbackPayload(ProcessEngineActor ProcessEngineActor, string Metadata);

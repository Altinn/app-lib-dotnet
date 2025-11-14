namespace Altinn.App.Core.Internal.ProcessEngine.Models;

/// <summary>
/// Represents the payload for process engine callbacks.
/// </summary>
/// <param name="ProcessEngineActor">The actor performing the process engine operation.</param>
/// <param name="Metadata">Additional metadata for the callback.</param>
public sealed record ProcessEngineCallbackPayload(ProcessEngineActor ProcessEngineActor, string Metadata);

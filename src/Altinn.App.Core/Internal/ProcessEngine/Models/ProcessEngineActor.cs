namespace Altinn.App.Core.Internal.ProcessEngine.Models;

/// <summary>
/// Represents an actor in the process engine.
/// </summary>
/// <param name="Language">The language preference of the actor.</param>
/// <param name="Identifier">The unique identifier for the actor.</param>
public sealed record ProcessEngineActor(string Language, string Identifier);

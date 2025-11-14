namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Represents the user/entity on whose behalf the process engine is executing tasks.
/// </summary>
/// <param name="Identifier">The actor's identifier, a UserId/PartyId or similar</param>
/// <param name="Language">Optional language code to associate with actions on behalf of this actor</param>
public sealed record ProcessEngineActor(string Identifier, string? Language = null);

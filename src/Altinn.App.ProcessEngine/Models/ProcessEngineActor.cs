namespace Altinn.App.ProcessEngine.Models;

// TODO: UserIdOrOrgNumber should probably be represented by a more specific type here. Eg. `Authenticated` or similar.
/// <summary>
/// Represents the user/entity on whose behalf the process engine is executing tasks.
/// </summary>
/// <param name="UserIdOrOrgNumber">The user-id or org number of the actor.</param>
/// <param name="Language">Optional language code to associate with actions on behalf of this actor</param>
public sealed record ProcessEngineActor(string UserIdOrOrgNumber, string? Language = null);

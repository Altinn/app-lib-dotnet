using System.Security.Claims;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Models.Process;

/// <summary>
/// Class that defines the request for moving the process to the next task
/// </summary>
public sealed record ProcessNextRequest
{
    // /// <summary>
    // /// Unique identifier of the organisation responsible for the app
    // /// </summary>
    // public required string Org { get; set; }
    //
    // /// <summary>
    // /// Application identifier which is unique within an organisation
    // /// </summary>
    // public required string App { get; set; }
    //
    // /// <summary>
    // /// Unique id of the party that is the owner of the instance
    // /// </summary>
    // public required int InstanceOwnerPartyId { get; set; }
    //
    // /// <summary>
    // /// Unique id to identify the instance
    // /// </summary>
    // public required Guid InstanceGuid { get; set; }

    /// <summary>
    /// The instance that is being processed
    /// </summary>
    public required Instance Instance { get; init; }

    /// <summary>
    /// The user that is performing the action
    /// </summary>
    public required ClaimsPrincipal User { get; init; }

    /// <summary>
    /// The action that is performed
    /// </summary>
    public required string? Action { get; init; }

    /// <summary>
    /// The organisation number of the party the user is acting on behalf of
    /// </summary>
    public string? ActionOnBehalfOf { get; set; }

    /// <summary>
    /// The language the user sent with process/next (not required)
    /// </summary>
    public required string? Language { get; init; }
}

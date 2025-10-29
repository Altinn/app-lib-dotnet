namespace Altinn.App.Core.Internal.App;

/// <summary>
/// Interface for accessing instance context information
/// </summary>
public interface IInstanceContext
{
    /// <summary>
    /// Instance Id
    /// </summary>
    string? InstanceId { get; set; }

    /// <summary>
    /// Party Id
    /// </summary>
    int? InstanceOwnerPartyId { get; set; }
}

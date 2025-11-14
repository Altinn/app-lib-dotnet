namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// Information about an instance.
/// </summary>
/// <param name="Org">The organization that the instance belongs to.</param>
/// <param name="App">The app that created the instance.</param>
/// <param name="InstanceOwnerPartyId">The instance owner's party ID.</param>
/// <param name="InstanceGuid">The instance ID.</param>
public sealed record InstanceInformation(string Org, string App, int InstanceOwnerPartyId, Guid InstanceGuid)
{
    /// <inheritdoc />
    public bool Equals(InstanceInformation? other)
    {
        if (other is null)
            return false;

        return Org.Equals(other.Org, StringComparison.OrdinalIgnoreCase)
            && App.Equals(other.App, StringComparison.OrdinalIgnoreCase)
            && InstanceOwnerPartyId == other.InstanceOwnerPartyId
            && InstanceGuid == other.InstanceGuid;
    }

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Org.ToLowerInvariant(), App.ToLowerInvariant(), InstanceOwnerPartyId, InstanceGuid);
};

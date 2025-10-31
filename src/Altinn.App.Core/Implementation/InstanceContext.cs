using Altinn.App.Core.Internal.App;

namespace Altinn.App.Core.Implementation;

/// <summary>
/// Scoped service to access instance information.
/// </summary>
public class InstanceContext : IInstanceContext
{
    private static readonly AsyncLocal<int?> _instanceOwnerPartyId = new();
    private static readonly AsyncLocal<string?> _instanceId = new();

    /// <summary>
    /// The instance id
    /// </summary>
    public string? InstanceId
    {
        get => _instanceId.Value;
        set => _instanceId.Value = value;
    }

    /// <summary>
    /// The party id
    /// </summary>
    public int? InstanceOwnerPartyId
    {
        get => _instanceOwnerPartyId.Value;
        set => _instanceOwnerPartyId.Value = value;
    }
}

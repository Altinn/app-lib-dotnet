using Altinn.App.Core.Features;

namespace Altinn.App.Core.Internal.App;

/// <summary>
/// Interface for getting custom layouts for an instance.
/// </summary>
[ImplementableByApps]
public interface ICustomLayoutForInstance
{
    /// <summary>
    /// Gets the custom layout
    /// </summary>
    /// <param name="layoutSetId">The layout set ID</param>
    /// <param name="instanceOwnerPartyId">The instance owner party ID</param>
    /// <param name="instanceGuid">The instance GUID</param>
    Task<string?> GetCustomLayoutForInstance(string layoutSetId, int instanceOwnerPartyId, Guid instanceGuid);
}

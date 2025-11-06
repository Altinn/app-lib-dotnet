using Altinn.App.Core.Features;

namespace Altinn.App.Core.Internal.App;

[ImplementableByApps]
public interface ICustomLayoutForInstance
{
    Task<string?> GetCustomLayoutForInstance(string layoutSetId, int instanceOwnerPartyId, Guid instanceGuid);
}

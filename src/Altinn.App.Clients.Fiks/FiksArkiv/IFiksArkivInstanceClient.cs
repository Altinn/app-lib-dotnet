using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal interface IFiksArkivInstanceClient
{
    /// <summary>
    /// Generates a <see cref="AuthenticationMethod.ServiceOwner()"/> JWT token.
    /// </summary>
    internal Task<JwtToken> GetServiceOwnerToken();

    /// <summary>
    /// Fetches the instance identified by the given <see cref="InstanceIdentifier"/>.
    /// </summary>
    Task<Instance> GetInstance(InstanceIdentifier instanceIdentifier);

    /// <summary>
    /// Moves the instance to the next process task, with a given action.
    /// </summary>
    Task ProcessMoveNext(InstanceIdentifier instanceIdentifier, string? action = null);

    /// <summary>
    /// Marks the instance as complete by the service owner.
    /// </summary>
    Task MarkInstanceComplete(InstanceIdentifier instanceIdentifier);

    /// <summary>
    /// Inserts binary data of a given type into the instance's data elements.
    /// </summary>
    Task<DataElement> InsertBinaryData<TContent>(
        InstanceIdentifier instanceIdentifier,
        string dataType,
        string contentType,
        string filename,
        TContent content,
        string? generatedFromTask = null
    );

    /// <summary>
    /// Deletes binary data from the instance's data elements.
    /// </summary>
    Task DeleteBinaryData(InstanceIdentifier instanceIdentifier, Guid dataElementGuid);
}

using Altinn.App.Core.Internal.Sign;

namespace Altinn.App.Core.Features.Signing.Interfaces;
/// <summary>
/// Interface for implementing app specific logic for deriving signees
/// </summary>
public interface ISigneeLogic{
    /// <summary>
    /// TODO: populate
    /// </summary>
    void Execute();
    /// <summary>
    /// Method to retrieve the signees for a task.
    /// </summary>
    /// <returns>A list of <see cref="Signee"/>. </returns>
    List<Signee> GetSignees();
}

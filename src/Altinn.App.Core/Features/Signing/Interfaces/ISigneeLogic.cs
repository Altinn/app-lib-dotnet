using System.Reflection.Metadata.Ecma335;
using Altinn.App.Core.Internal.Sign;

namespace Altinn.App.Core.Features.Signing.Interfaces;
/// <summary>
/// TODO: populate
/// </summary>
public interface ISigneeLogic{
    /// <summary>
    /// TODO: populate
    /// </summary>
    void Execute();
    /// <summary>
    /// TODO: populate
    /// </summary>
    /// <returns></returns>
    List<Signee> GetSignees();
}

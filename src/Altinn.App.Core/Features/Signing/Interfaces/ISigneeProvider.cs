using Altinn.App.Core.Features.Signing.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for implementing app-specific logic for deriving signees.
/// </summary>
[ImplementableByApps]
public interface ISigneeProvider
{
    /// <summary>
    /// Used to select the correct <see cref="ISigneeProvider" /> implementation for a given signing task.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// Returns a list of signees for the current signing task.
    /// </summary>
    Task<SigneesResult> GetSigneesAsync(Instance instance);
}

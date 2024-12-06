using Altinn.App.Core.Features.Signing.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

/// <summary>
/// Interface for implementing app-specific logic for deriving signees.
/// </summary>
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

/// <summary>
/// A result containing persons and organizations that should sign and related info for each of them.
/// </summary>
public class SigneesResult
{
    /// <summary>
    /// The signees who are persons that should sign.
    /// </summary>
    public required List<PersonSignee> PersonSignees { get; set; }

    /// <summary>
    /// The signees who are organizations that should sign.
    /// </summary>
    public required List<OrganisationSignee> OrganisationSignees { get; set; }
}

namespace Altinn.App.Clients.Fiks.FiksIO.Models;

/// <summary>
/// Represents the non-sensitive settings for a FIKS IO account.
/// </summary>
public interface IFiksIOAccountSettings
{
    /// <summary>
    /// The account ID.
    /// </summary>
    Guid AccountId { get; }

    /// <summary>
    /// The integration ID.
    /// </summary>
    Guid IntegrationId { get; }
}

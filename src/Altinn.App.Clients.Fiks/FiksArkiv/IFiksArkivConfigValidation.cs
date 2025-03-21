namespace Altinn.App.Clients.Fiks.FiksArkiv;

/// <summary>
/// Interface for validating the configuration of the FIKS Arkiv client.
/// </summary>
public interface IFiksArkivConfigValidation
{
    /// <summary>
    /// Validates the configuration of the FIKS Arkiv client.
    /// </summary>
    Task ValidateConfiguration();
}
